# GPT-DEV-09 独立审计报告 R1

## 一、审计范围

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-09-runtime-bootstrap-plugin-entry.md`
- SourceSet：`BUE-SS-20260824-02`
- 审计对象：
  - `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`
  - `src/BetterUnturnedExperience.Plugin/BootstrapGuard.cs`
  - `src/BetterUnturnedExperience.Plugin/PluginAssemblyMarker.cs`
  - `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj`
  - `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`
  - `BetterUnturnedExperience.sln`
- 审计性质：独立静态审计、独立构建、独立测试；不包含真实游戏、SteamP2PFriends 或 U3DS 运行验收。

## 二、最终判定

**FAIL（1 个阻断项）**

DEV-09 暂不得标记 `resolved`，也不得宣称 BepInEx、单人、P2P 或 U3DS 实际加载通过。修复阻断项后，应重新执行构建、测试和独立审计。

## 三、阻断项

### B-DEV09-01：启动日志未显式记录 FeatureId

- **涉及文件与位置**：
  - `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs:6-21`
- **问题**：工单验收条件要求启动异常记录 `FeatureId`、`DiagnosticId` 与状态。当前代码仅在日志中输出可读文本 `Better Unturned Experience`、决策值和 `BUE-BOOTSTRAP-001`；没有输出机器可判定的永久身份 `io.github.yu80rice.betterunturnedexperience`。异常分支也没有状态字段。
- **根因**：插件身份只写在 `[BepInPlugin]` 属性中，没有在运行时诊断 seam 中复用同一个常量并结构化输出。
- **修复建议**：在插件入口建立唯一 `FeatureId` 常量，属性与日志共同使用；正常和异常路径至少输出 `featureId=<...>`, `diagnosticId=<...>`, `status=<decision/isolated>`。不要引入新的身份来源或可变项目名。

## 四、逐项审计结果

| 审计项 | 判定 | 证据 |
|---|---|---|
| 唯一 BepInEx 入口 | PASS | 已加载 Release DLL 的 4 个类型中仅 `BetterUnturnedExperience.Plugin.BetterUnturnedExperiencePlugin` 带 `[BepInPlugin]`；没有第二个入口。 |
| GUID / 预发布版本 | PASS | `BetterUnturnedExperiencePlugin.cs:6` 为 `io.github.yu80rice.betterunturnedexperience` 与 `0.0.0`。反射读取到完全相同的三个构造参数。 |
| BepInEx 版本绑定 | PASS | Release DLL 引用 `BepInEx, Version=5.4.23.5`；参考文件为 `DevelopMyUNMultiplayerModAndModloader/Libs/BepInEx.dll`。 |
| BootstrapGuard 分流 | PASS | `BootstrapGuard.cs:7-10` 对 batch/headless 返回 `Headless`，UI 不可用返回 `Unavailable`，否则返回 `Client`；测试覆盖四个分支。 |
| Awake 异常隔离 | PASS（实现层） | `BetterUnturnedExperiencePlugin.cs:13-22` 捕获异常并记录错误，不向外抛出；实际 CLR/进程可继续性仍需运行验证。 |
| Harmony / 库存 / UI / LMN | PASS | Plugin 源码没有 Harmony、PatchAll、原生库存、Glazier/Sleek 或 LMN 接入；本票未提前实现功能。 |
| Contracts/Core/Release 引擎隔离 | PASS（语义扫描） | 反射检查：Contracts 仅引用 mscorlib；Core 仅引用 mscorlib/System/System.Core/Contracts；Release 仅引用 mscorlib/System/System.Core。 |
| Headless 不实例化 ClientUi | PASS（静态范围） | DEV-09 Plugin 工程未引用 ClientUi，也没有 UI 工厂或反射扫描；真实 U3DS 加载仍未验证。 |
| .NET/C# 基线 | PASS | Plugin `.csproj` 为 .NET Framework 4.7.2、C# 10；solution Release Rebuild 成功。 |

## 五、独立验证记录

### 1. 构建

命令：

```text
dotnet msbuild D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：**PASS，0 errors / 0 warnings**。

### 2. 测试

以下测试全部退出码 0：

- `DEV-02 definition linker tests: PASS`
- `DEV-03 settings runtime tests: PASS`
- `DEV-04 placement evaluator tests: PASS`
- `DEV-05 ClientUi tests: PASS`
- `DEV-06 network tests: PASS`
- `DEV-08 runtime evidence package tests: PASS`
- `DEV-09 runtime bootstrap tests: PASS`

### 3. 产物摘要

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `46FA1E779EE8784D59FA9DC9E3D375496F94002A9E3F051A788290B900EB404B` |
| `tests/BetterUnturnedExperience.Plugin.Tests/bin/Release/BetterUnturnedExperience.Plugin.Tests.exe` | `48531420FB94D26ADB784F802B2A65D1D7F037EFB387E829C2CEEA10BF9B1E58` |

### 4. 静态扫描说明

`eng/Verify-NoUiTokens.ps1` 对 Contracts/Core 通过。对 Release 的通用文本扫描会命中 `Qualification.cs` 的字段名 `BepInExVersion`；该命中是字段名而非程序集/类型引用。以 Release DLL 的程序集引用和类型元数据进行语义复核后，未发现 BepInEx、Unity、Glazier、Sleek、Unturned、LMN 或 Harmony 引用。

## 六、未验证项与边界

- 未在真实客户端 BepInEx 环境执行插件发现、实例化和日志验证。
- 未在真实 U3DS Headless 环境执行同 DLL 加载验证。
- 未验证单人、SteamP2PFriends Host/Client、原生库存链或物品交互。
- 本报告不授予 `ReleaseReady`、`Stable` 或正式发布授权。

## 七、非阻断建议

1. `Awake` 当前以 `Application.isBatchMode` 同时作为 `headless` 和 `clientUiAvailable` 的替代输入。BootstrapGuard seam 已支持独立输入，但真实 UI 能力检测与 U3DS 运行验证应留给后续运行时门禁；修复 B-DEV09-01 时不要把它扩展为隐式 UI 加载。
2. 建议将 `FeatureId`、`DiagnosticId` 和状态日志格式集中为稳定的键值字段，便于后续 DEV-08 证据采集和诊断解析。

## 八、审计结论

DEV-09 的最小 BepInEx 入口、静态隔离和构建测试基础已成立，但因诊断日志未满足永久 FeatureId 记录的明确验收条件，R1 判定 **FAIL**。修复后必须重新编译并重新执行本审计。
