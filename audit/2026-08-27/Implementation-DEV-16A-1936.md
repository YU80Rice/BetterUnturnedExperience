# DEV-16A 单 DLL Composition Root 实施报告

## 一、需求执行概述

将纯 C# ClientUi Seam 编入 `BetterUnturnedExperience.dll`，由唯一 Composition Root 在客户端组合官方功能；BatchMode、Headless 与原生 UI 不可用时在 UI 工厂前 Fail-Closed。

## 二、源码溯源清单

| 需求点 | 落实位置 |
| --- | --- |
| 主 DLL 嵌入 Contracts/Core/ClientUi Seam | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj` |
| 官方注册返回非空 ClientUi Satellite | `src/BetterUnturnedExperience.Plugin/OfficialFeatureRegistration.cs` |
| 客户端唯一 Composition Root 与生命周期 | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`、`ClientUiCompositionRoot.cs` |
| Client/BatchMode/Headless/Unavailable 分流 | `BetterUnturnedExperiencePlugin.Awake`、`BueClientUiCompositionRoot.Initialize` |
| 初始化幂等、销毁与 SafeMode 隔离 | `src/BetterUnturnedExperience.ClientUi/ClientUiTypes.cs`、插件 `OnDestroy` |
| TDD 门禁 | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` |

## 三、代码变更清单

- 修改 `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`：接入客户端 Composition Root，增加销毁隔离。
- 修改 `src/BetterUnturnedExperience.Plugin/OfficialFeatureRegistration.cs`：提供官方 ClientUi Satellite 元数据。
- 新增/纳入 `src/BetterUnturnedExperience.Plugin/ClientUiCompositionRoot.cs` 与单 DLL 链接编译项。
- 修改 `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`：增加重复初始化、原生 UI 不可用和 Headless 零工厂调用断言。

## 四、编译与测试验证记录

- 命令：`MSBuild.exe BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal`
- 结果：`0 errors / 0 warnings`。
- 7 个 `bin/Release` 测试程序全部退出码 `0` 并输出 `PASS`；详见 [DEV-16A-tests.log](./DEV-16A-tests.log)。
- Contracts/Core/ClientUi 静态 UI/native token 扫描全部 PASS；详见同目录 `DEV-16A-token-scan-*.log`。
- 主 DLL AssemblyRef 仅包含宿主依赖 `BepInEx`、`UnityEngine.CoreModule` 与 BCL，不包含独立 Contracts/Core/ClientUi。
- `git diff --check` 通过。

## 五、独立审计记录

- 审计代理：`dev16_factcheck`。
- 判定：`PASS`，无阻断项。
- 复核确认：单 DLL ABI 闭包、四类环境门禁、官方 Satellite、初始化幂等、销毁清理及未修改 U3-SDK/Unturned/SteamP2PFriends。
- 非阻断边界：DEV-16A 尚未实现真实 Glazier/Sleek/Harmony/库存 Hook；`nativeUiAvailable=true` 的真实探测属于 DEV-16B～D，不能宣称真实 UI 或玩法已通过。

## 六、产物哈希

| 产物 | SHA-256 |
| --- | --- |
| `BetterUnturnedExperience.Plugin.csproj` | `1046056148F8F9949A9F3FF57727CFBC0DD5D49902A975353B68BEB0847FB837` |
| `ClientUiCompositionRoot.cs` | `03F25D9D4BE03472631D4FA5CD6D9BF7E87A622298DB1D2A9DD2EE14B8A6F940` |
| `BetterUnturnedExperiencePlugin.cs` | `A429590F7DEAA5310D5FC2B8BE7D0C121A1FB1F0F0F7353CEE0A1C3CE4A08691` |
| `OfficialFeatureRegistration.cs` | `2505378B8B57144B908ABA2080287CF128A1A24AA21F61C9D15CAB5244E7D314` |
| `Program.cs` | `8FA087643ADD70A81EC83B53B43F5BC0D6354C67136D49C319536EB8132A4F9F` |
| `BetterUnturnedExperience.dll` | `EBC1D3080D44D3C5D6A19B5E2EC6355CE9FAB99CC568FAE1B2DEEB2415965CC8` |

## 七、偏离与妥协说明

无偏离。真实原生能力探测和 UI/库存接线按规格留给 DEV-16B～D。

## 八、结论与下一步

规格复核提出的场景订阅对称性建议已修复：`BetterUnturnedExperiencePlugin.OnDestroy` 现在会在任何运行阶段退订 `SceneManager.sceneLoaded`，并已重新编译和测试。DEV-16A 静态、TDD、编译和独立审计闭环通过，可交 Gemini 前端消费复核并进入 DEV-16B。当前不等同于真实客户端 UI、库存拖拽或三环境资格通过。
