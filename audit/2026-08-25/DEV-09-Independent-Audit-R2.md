# GPT-DEV-09 独立后端审计报告 R2

## 一、审计范围

- 工单：`.scratch/better-unturned-experience-architecture/issues/DEV-09-runtime-bootstrap-plugin-entry.md`
- SourceSet：`BUE-SS-20260824-02`
- 基线：`BUE-V1-RT01-20260824`
- 对照报告：`audit/2026-08-25/DEV-09-Independent-Audit-R1.md`
- 审计性质：独立静态审计、独立 Release 重建、独立测试与程序集元数据审计；不包含真实游戏、SteamP2PFriends 或 U3DS 运行验收。
- 本轮约束：未修改生产代码。

## 二、最终判定

**PASS（R1 阻断项已修复，DEV-09 具备交 Gemini 复核的静态交付条件）**

本 PASS 仅覆盖工单要求的代码、构建、测试和静态隔离门禁。它不等同于 BepInEx 实际加载、单人、SteamP2PFriends P2P、U3DS Headless 或 Better Item Interaction 功能通过。

## 三、R1 阻断项复核

### B-DEV09-01：FeatureId/status 结构化启动日志

**判定：PASS，已修复。**

- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs:6-11` 建立唯一常量：
  - `FeatureId = io.github.yu80rice.betterunturnedexperience`
  - `DiagnosticId = BUE-BOOTSTRAP-001`
- 同一 `FeatureId` 用于 `[BepInPlugin]` 属性和运行时日志，未出现第二身份来源。
- 正常路径 `:22` 输出 `featureId=... status=BootstrapReady ... diagnosticId=...`。
- 异常路径 `:26` 输出 `featureId=... status=BootstrapFailed diagnosticId=... errorType=...`，并捕获异常不向外抛出。
- 因此满足“异常 fail-closed、记录 FeatureId、DiagnosticId 与状态”的明确要求。

## 四、逐项审计结果

| 审计项 | 判定 | 证据 |
|---|---|---|
| TDD / BootstrapGuard seam | PASS（静态与测试） | `src/BetterUnturnedExperience.Plugin/BootstrapGuard.cs:3-11`；`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:14-18` 覆盖 Client、Headless、显式 Headless、Unavailable 四分支。 |
| 唯一 BepInEx 入口 | PASS | Release DLL 反射读取 `[BepInPlugin]` 类型计数为 1：`BetterUnturnedExperience.Plugin.BetterUnturnedExperiencePlugin`。 |
| GUID / 预发布版本 | PASS | `[BepInPlugin]` 反射值为 `io.github.yu80rice.betterunturnedexperience`、`Better Unturned Experience`、`0.0.0`；源码位置 `BetterUnturnedExperiencePlugin.cs:6`。 |
| Headless / UI 分流 | PASS（静态范围） | `BootstrapGuard.Decide` 对 `isBatchMode || headless` 先行返回 `Headless`；Plugin 工程不引用 `ClientUi`，入口不创建 UI 工厂；真实 U3DS 行为仍未验证。 |
| 启动异常隔离 | PASS（实现层） | `BetterUnturnedExperiencePlugin.cs:12-27` 使用 `try/catch`，错误记录后不再抛出；CLR/进程能否继续需运行环境验证。 |
| 未提前实现功能 | PASS | DEV-09 入口未注册 Harmony、未解析原生库存、未接入 Glazier/Sleek、未修改 LMN。 |
| Contracts/Core/Release 引擎隔离 | PASS（程序集引用语义） | Release DLL 参考仅为 `mscorlib/System.Core`；Core 参考为 BCL 与 `BetterUnturnedExperience.Contracts`；Contracts 仅 `mscorlib`。 |
| Plugin 边界依赖 | PASS | Plugin 仅额外引用 `BepInEx, Version=5.4.23.5`、`UnityEngine.CoreModule`，符合允许的 Plugin 边界；没有 ClientUi/LMN/原生库存程序集引用。 |
| .NET/C# 基线 | PASS | `BetterUnturnedExperience.Plugin.csproj:6` 为 .NET Framework 4.7.2、C# 10、Release `TreatWarningsAsErrors=true`。 |
| 静态 UI/native token 门禁 | PASS（目标层） | `eng/Verify-NoUiTokens.ps1` 对 Contracts、Core 通过；Release 源码扫描唯一命中为 `Qualification.cs` 字段名 `BepInExVersion`，程序集引用语义审计确认无 BepInEx 类型引用。 |

## 五、独立验证记录

### 1. Release 构建

命令：

```text
dotnet msbuild .\BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：**PASS，0 errors / 0 warnings**。构建输出覆盖 Contracts、Core、Plugin、ClientUi、Transport、Release、NoOpFixture 及测试项目。

### 2. 测试

以下现有 Release 测试进程全部退出码 0：

```text
DEV-10 registration runtime tests: PASS
DEV-03 settings runtime tests: PASS
DEV-04 placement evaluator tests: PASS
DEV-05 ClientUi tests: PASS
DEV-06 network tests: PASS
DEV-08 runtime evidence package tests: PASS
DEV-11 external fixture tests: PASS
```

其中 `BetterUnturnedExperience.Plugin.Tests` 同时执行了 `BootstrapGuard` 四分支断言（`Program.cs:14-18`）及外部 Fixture 注册链测试；当前测试输出标签为 DEV-11，未将其误报为真实 BepInEx 运行通过。

### 3. 入口与程序集元数据

对 `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` 的反射验证结果：

- `[BepInPlugin]` 类型数：`1`
- 类型：`BetterUnturnedExperience.Plugin.BetterUnturnedExperiencePlugin`
- GUID：`io.github.yu80rice.betterunturnedexperience`
- 版本：`0.0.0`
- Plugin 程序集引用：`mscorlib`、`BetterUnturnedExperience.Core`、`BetterUnturnedExperience.Contracts`、`BepInEx 5.4.23.5`、`UnityEngine.CoreModule`

### 4. 产物摘要

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `4B0AEC997496994CEFBF15441063E1CD3CBF481B5116C4CAEF035D3CB9A5077E2` |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `1FCB65E8620D39DF3B7F6A8DF65C642F15128348B2A5A87E55D82544339DE8CD` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `630B86C7B4713994677D431C14F52D8C9678949B91725CE7B81259273B9ABECF` |
| `src/BetterUnturnedExperience.Release/bin/Release/BetterUnturnedExperience.Release.dll` | `1F4451D0FBE238AE9D5EA4E6BE0CD5CA0880F5D10D1F519FE230D489FF3CE42A` |

## 六、残余风险与边界

- 尚未在真实客户端 BepInEx 5.23 clean-install 中验证插件发现、实例化及日志落盘。
- 尚未在真实 U3DS Headless 环境验证同一 DLL 的加载与进程继续性。
- 尚未验证单人、SteamP2PFriends Host/Client、原生库存链或物品交互。
- `Awake` 当前以 `Application.isBatchMode` 作为 `headless` 与 UI 可用性的输入替代；这是本票最小入口的静态实现限制，不能替代后续真实运行时门禁。
- `eng/Verify-NoUiTokens.ps1` 对 Release 的源码扫描会把 `Qualification.cs` 中的 `BepInExVersion` 字段名视作文本命中；本轮以程序集引用与类型元数据进行语义审计，未发现引擎类型泄漏。后续可将扫描器升级为引用语义扫描，但不构成本轮阻断。

## 七、审计结论与交接

R1 唯一阻断项已闭环，当前 DEV-09 代码、构建、测试、唯一入口、预发布版本、Headless 分流和层级隔离均通过静态审计。建议将本报告交给 Gemini 进行前端消费与 Headless 边界复核，并在其 ACCEPT 且人工确认后进入 BepInEx clean-install 冒烟测试。

本报告不授予 `ReleaseReady`、`Stable`、真实运行通过或三环境验收通过。

