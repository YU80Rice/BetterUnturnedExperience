# GPT-DEV-12 独立审计 R2

- 审计对象：DEV-12「BUE 单 DLL 运行时程序集闭包」
- 基线：BUE-V1-RT01-20260824
- SourceSet：BUE-SS-20260824-02
- 审计时间：2026-08-25
- 审计类型：B-01 修复后的独立复测

## 一、最终判定

**判定：PASS（静态、构建与测试审计通过；工单仍保持 `ready-for-human`）**

R1 的 B-01 已闭环。SDK 身份规则现已明确冻结为：第三方编译期与运行期均绑定 `BetterUnturnedExperience.dll`；独立 `BetterUnturnedExperience.Contracts.dll` 仅作为内部 compile-time/test artifact，不得进入玩家运行时部署。Plugin.Tests 已新增外部 Fixture AssemblyRef 回归断言，确认外部 Fixture 不绑定 Contracts/Core，而绑定 BUE 主程序集。

本轮没有发现新的规格阻断项。真实客户端 clean-install 冒烟仍未由本轮完成，因此不能将 DEV-12 标记为 `resolved`，也不能宣称 U3DS、SP、SteamP2PFriends P2P 或 Better Item Interaction 运行通过。

## 二、R1 阻断项 B-01 复核

| 检查点 | 判定 | 证据 |
|---|---|---|
| 编译期/运行期唯一身份策略 | PASS | `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` 明确第三方引用 `BetterUnturnedExperience.dll`，并禁止部署 Contracts/Core DLL。 |
| 内部 Contracts artifact 边界 | PASS | 文档明确 `BetterUnturnedExperience.Contracts.dll` 仅供隔离测试、源码审查和内部编译验证，不是玩家依赖或第三方 ABI DLL。 |
| 外部 Fixture AssemblyRef 回归 | PASS | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` 的 `AssertExternalSdkAssemblyIdentity()` 拒绝 Contracts/Core 引用并要求存在 `BetterUnturnedExperience` 引用；测试实测 PASS。 |
| 公共 ABI 单一事实源 | PASS | `typeof(FeatureId).Assembly == typeof(BueRuntimeHost).Assembly` 断言通过；No-op Fixture IL 的 Contracts 类型均解析为 `[BetterUnturnedExperience]`。 |

## 三、程序集闭包与 ABI 复核

### BUE 主 DLL

`BetterUnturnedExperience.dll` 的 `Assembly.GetReferencedAssemblies()` 为：

```text
mscorlib, 4.0.0.0
BepInEx, 5.4.23.5
System.Core, 4.0.0.0
System, 4.0.0.0
UnityEngine.CoreModule, 0.0.0.0
```

不存在 `BetterUnturnedExperience.Core` 或 `BetterUnturnedExperience.Contracts` AssemblyRef。BepInEx/Unity 为宿主依赖例外，符合 DEV-12 依赖闭包定义。

### No-op Fixture

`BetterUnturnedExperience.NoOpFixture.dll` 的直接 AssemblyRef 为：

```text
mscorlib, 4.0.0.0
BepInEx, 5.4.23.5
BetterUnturnedExperience, 0.0.0.0
```

不存在 Contracts/Core AssemblyRef。IL 中 `FeatureRegistrationResult`、`FeatureId`、`IFeatureRegistration`、`BueRuntimeHost` 等均通过 `[BetterUnturnedExperience]` 解析。

### ClientUi 卫星

`BetterUnturnedExperience.ClientUi.dll` 直接引用 `BetterUnturnedExperience, 0.0.0.0`，不反向要求 Contracts/Core；BUE 主 DLL 不反向引用 ClientUi，保持可选卫星与 Headless 物理隔离方向。ClientUi 真实运行仍不在本票证据范围内。

## 四、Headless 与静态边界复核

对 Contracts/Core/Release 源码及项目引用进行扫描，未发现以下生产类型或发现入口：

```text
UnityEngine（Contracts/Core/Release）
Glazier
Sleek
SDG.Unturned
LMN
Harmony
Steamworks
Assembly.GetTypes
Assembly.GetAssemblies
PatchAll
Directory.GetFiles / Directory.EnumerateFiles
```

主 DLL 的 Unity/BepInEx 引用仅存在于插件入口层；Contracts/Core/Release 仍保持无 UI/native/LMN/BepInEx 类型泄漏。

## 五、构建与测试复测

### Release 构建

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /m /v:minimal
```

结果：**成功，0 errors / 0 warnings**。

### 测试结果

7 个 Release 测试项目全部 PASS：

| 测试项目 | 输出 |
|---|---|
| BetterUnturnedExperience.Settings.Tests | `DEV-03 settings runtime tests: PASS` |
| BetterUnturnedExperience.Placement.Tests | `DEV-04 placement evaluator tests: PASS` |
| BetterUnturnedExperience.ClientUi.Tests | `DEV-05 ClientUi tests: PASS` |
| BetterUnturnedExperience.Network.Tests | `DEV-06 network tests: PASS` |
| BetterUnturnedExperience.Release.Tests | `DEV-08 runtime evidence package tests: PASS` |
| BetterUnturnedExperience.Contracts.Tests | `DEV-10 registration runtime tests: PASS` |
| BetterUnturnedExperience.Plugin.Tests | `DEV-11 external fixture tests: PASS`，包含 DEV-12 外部 SDK 身份回归断言 |

## 六、本轮产物哈希

| 产物 | SHA-256 |
|---|---|
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `F8FCAA0E42098561D0D38DB67782F4F94ECB7633E2A68B3DDF88D1C614149E8C` |
| `src/BetterUnturnedExperience.NoOpFixture/bin/Release/BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` |
| `src/BetterUnturnedExperience.ClientUi/bin/Release/BetterUnturnedExperience.ClientUi.dll` | `A2F128B444A0A851D2C8E2B09DE913D1FFB7958AAE4C982590E57C843F13E852` |

## 七、剩余验证边界与推进裁定

R2 审计 PASS 后，可以将 GPT 独立审计门禁视为通过，并交付 Gemini 做前端公开 ABI/Headless 消费复核。DEV-12 工单仍应保持 `ready-for-human`，等待人工在真实 BepInEx clean-install 中仅部署 `BetterUnturnedExperience.dll`，随后以 BUE 主 DLL + No-op Fixture DLL 验证公开注册链。

本报告不证明：U3DS、单人、SteamP2PFriends Host/Client、三环境资格、ClientUi 真实装配或 Better Item Interaction 玩法运行通过。
