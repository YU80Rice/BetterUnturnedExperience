# DEV-12 单 DLL 运行时程序集闭包实施报告

## 一、需求执行概述

将 BUE 的 Contracts/Core 源码以链接编译方式嵌入 `BetterUnturnedExperience.Plugin`，使最终 `BetterUnturnedExperience.dll` 自包含 BUE 公开契约与核心运行时；No-op Fixture 与 ClientUi 改为绑定主程序集公开 ABI。

## 二、源码溯源清单

| 需求点 | 落实位置 |
|---|---|
| 单 DLL 源码聚合 | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj` 的 `EmbeddedContracts` / `EmbeddedCore` Compile Link 项 |
| 公开契约运行时身份唯一化 | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs::AssertSingleDllAssemblyClosure` |
| 主 DLL 不依赖私有 Core/Contracts | 同上；测试枚举 `BetterUnturnedExperiencePlugin` AssemblyRef |
| No-op 外部插件绑定主 ABI | `src/BetterUnturnedExperience.NoOpFixture/BetterUnturnedExperience.NoOpFixture.csproj`、`NoOpFeaturePlugin.cs` |
| ClientUi 卫星绑定主 ABI | `src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj` |
| 干净单 DLL staging | `artifacts/DEV-12-clean-single-dll-20260825/BetterUnturnedExperience.dll` |

## 三、变更文件

- `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj`
- `src/BetterUnturnedExperience.NoOpFixture/BetterUnturnedExperience.NoOpFixture.csproj`
- `src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`
- `src/BetterUnturnedExperience.ClientUi/BetterUnturnedExperience.ClientUi.csproj`
- `tests/BetterUnturnedExperience.Plugin.Tests/BetterUnturnedExperience.Plugin.Tests.csproj`
- `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`
- `.scratch/better-unturned-experience-architecture/issues/DEV-12-single-dll-runtime-assembly-closure.md`

## 四、TDD 记录

- Red：旧主 DLL 的 `AssemblyRef` 包含 `BetterUnturnedExperience.Core` 和 `BetterUnturnedExperience.Contracts`；仅主 DLL 依赖闭包检查失败。
- Green：源码聚合后主 DLL 不再引用两项私有程序集；新增测试验证公开 `FeatureId`、`IFeatureRegistration` 与 `BueRuntimeHost` 来自同一主程序集。
- 重构：No-op Fixture 与 ClientUi 项目引用切换到主程序集，未引入运行时扫描或 IL 合并器。

## 五、编译与测试

命令：

```text
dotnet msbuild D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\更好的UN体验\BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：**0 errors / 0 warnings**。

现有 7 个测试项目全部 PASS：DEV-03、DEV-04、DEV-05、DEV-06、DEV-08、DEV-10、DEV-11。

## 六、程序集闭包验证

主 DLL 程序集引用：`BepInEx`、`UnityEngine.CoreModule`、`System`、`System.Core`、`mscorlib`；不包含 `BetterUnturnedExperience.Core` 或 `BetterUnturnedExperience.Contracts` AssemblyRef。

No-op Fixture 程序集引用：`BepInEx`、`BetterUnturnedExperience`、`mscorlib`；不引用私有 Core/Contracts。

ClientUi 程序集引用：`BetterUnturnedExperience`、`mscorlib`。

## 七、产物哈希

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C` |
| `src/BetterUnturnedExperience.NoOpFixture/bin/Release/BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` |
| `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll` | `A2F128B444A0A851D2C8E2B09DE913D1FFB7958AAE4C982590E57C843F13E852` |

## 九、SDK 身份策略修复

R1 独立审计发现：独立 Contracts DLL 的存在可能误导第三方形成悬空运行时 AssemblyRef。已冻结并文档化唯一策略：第三方编译期与运行期均直接引用 `BetterUnturnedExperience.dll`；`BetterUnturnedExperience.Contracts.dll` 仅用于仓库内部隔离测试和源码验证，不得进入玩家部署或第三方运行时 LoadSet。

新增回归断言 `AssertExternalSdkAssemblyIdentity`，验证 No-op Fixture：

- 存在 `BetterUnturnedExperience` AssemblyRef；
- 不存在 `BetterUnturnedExperience.Contracts` / `BetterUnturnedExperience.Core` AssemblyRef；
- 公开契约类型与 `BueRuntimeHost` 来自同一 `BetterUnturnedExperience` Assembly。

规则文档：`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`。

## 八、边界与待验证项

- 已完成静态/构建/单元测试闭环；主 DLL clean staging 仅含一个 BUE DLL。
- 尚未覆盖真实客户端仅主 DLL 的 BepInEx 启动日志；需要人工部署后确认 `BootstrapReady`。
- 尚未证明 U3DS、SteamP2PFriends Host/Client、Better Item Interaction 或三环境资格。
- DEV-12 在独立审计与人工 clean-install 之前不得标记 `resolved`。

## 九、独立审计

- R1：FAIL，阻断 B-01 为 SDK/Contracts 编译期与运行期身份策略未冻结。
- 修复：新增 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`，冻结第三方只引用 BUE 主程序集；新增外部 Fixture AssemblyRef 回归断言。
- R2：PASS，报告 `audit/2026-08-25/DEV-12-Independent-Audit-R2.md`。
- 当前结论：可交 Gemini 复核并进入人工单 DLL clean-install 冒烟；真实冒烟完成前保持 `ready-for-human`。

