# DEV-11 实施报告：No-op 外部功能 Fixture 与 BUE Host 注册桥

## 一、需求执行概述

完成独立 BepInEx no-op 功能夹具与 BUE Host public registration bridge 的最小垂直切片，证明外部功能可在 BUE 初始化后通过公开注册 seam 登记，并在 Host 缺失或 Catalog 冻结后 fail-closed。

## 二、源码溯源

| 需求点 | 落实位置 |
| --- | --- |
| BUE public registration bridge | `src/BetterUnturnedExperience.Plugin/BueRuntimeHost.cs`：`Phase`、`Register`；`Bind/Clear` 为 internal |
| BUE Awake 初始化 Host | `src/BetterUnturnedExperience.Plugin/BetterUnturnedExperiencePlugin.cs`：创建 `FeatureRegistrationRuntime`、绑定并打开 Registration phase |
| 独立 BepInEx Fixture | `src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`：插件属性、HardDependency、`Awake` 调用链 |
| 无私有 Core 依赖 | `BetterUnturnedExperience.NoOpFixture.csproj` 与 Fixture IL/AssemblyRef 检查 |
| Host 缺失/成功/晚注册测试 | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` |

## 三、TDD 记录

- Red：先将公开桥和 no-op fixture 测试写入 Plugin.Tests；在 fixture 工程和桥实现不存在时得到缺失引用/编译失败。
- Green：加入 `BueRuntimeHost`、Plugin Awake 初始化、独立 NoOpFixture 工程和注册链；测试通过。
- 修复轮：独立审计 R1 发现 Fixture 没有实际 `Awake` 注册；补齐 `NoOpFeaturePlugin.Awake`，并用 `NoOpFeatureBootstrap.Awake` 作为可测试生命周期 seam，重新构建和复测。

## 四、编译与测试

命令：

```text
dotnet msbuild BetterUnturnedExperience.sln /t:Rebuild /p:Configuration=Release /v:minimal
```

结果：0 errors / 0 warnings。

7 个测试项目全部 PASS：ClientUi、Contracts/DEV-10、Network、Placement、Plugin/DEV-11、Release、Settings。

## 五、静态与装配检查

- Contracts UI/native token scan：PASS。
- Core UI/native token scan：PASS。
- Fixture 源码无 `Assembly.GetTypes`、`GetAssemblies`、目录扫描、`LoadFrom`、`PatchAll` 或 `IFeatureModule.Start`。
- Fixture Release 输出目录仅包含 `BetterUnturnedExperience.NoOpFixture.dll`，不会携带 Contracts/Core 隐式副本。
- Fixture 无 `BetterUnturnedExperience.Core` ProjectReference；AssemblyRef 直接依赖 BepInEx、BUE Host、Contracts 与 mscorlib。

## 六、独立审计

- R1：[DEV-11-Independent-Audit-R1.md](DEV-11-Independent-Audit-R1.md) FAIL，阻断 B-01：未覆盖真实 Fixture Awake 注册。
- R2：[DEV-11-Independent-Audit-R2.md](DEV-11-Independent-Audit-R2.md) PASS，确认 Awake 链、依赖闭包、构建、测试和静态门禁全部通过。

## 七、最终产物哈希

| 产物 | SHA-256 |
| --- | --- |
| `src/BetterUnturnedExperience.Plugin/bin/Release/BetterUnturnedExperience.dll` | `4B0AEC997496994CEFBF15441063E1CD3CBF481B5116C4CAEF035D3CB9A5077E2` |
| `src/BetterUnturnedExperience.NoOpFixture/bin/Release/BetterUnturnedExperience.NoOpFixture.dll` | `4C5EB8E6A65DA58AB7A1669E328BDB3652CEA0037E72EE719A0D6FA92FEA4BA7` |
| `tests/BetterUnturnedExperience.Plugin.Tests/bin/Release/BetterUnturnedExperience.Plugin.Tests.exe` | `490D1F49A5944FD1C7FE2E591246A6AB0C3B64E83E3AF5260B3259F1ABF46073` |
| `src/BetterUnturnedExperience.Contracts/bin/Release/BetterUnturnedExperience.Contracts.dll` | `1FCB65E8620D39DF3B7F6A8DF65C642F15128348B2A5A87E55D82544339DE8CD` |
| `src/BetterUnturnedExperience.Core/bin/Release/BetterUnturnedExperience.Core.dll` | `630B86C7B4713994677D431C14F52D8C9678949B91725CE7B81259273B9ABECF` |

## 八、证据边界

本票只证明源码调用链、静态装配、测试和构建成立；尚未证明真实 BepInEx clean-install/Chainloader 日志、U3DS/SP/P2P 运行、ClientUi satellite、LoadSetIdentity、Better Item Interaction 或发布资格。

