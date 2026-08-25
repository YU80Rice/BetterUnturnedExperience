# DEV-14 官方 Better Item Interaction 公开注册实施报告

## 一、需求执行概述

依据 `DEV-14-official-better-item-interaction-registration.md` 与公开运行时框架规格，将官方 Better Item Interaction 作为 BUE Host 内置功能登记，并使用与第三方 No-op Fixture 相同的公开注册桥与 Catalog barrier。

本票是 registration tracer bullet，不实现原生库存 Hook、Glazier UI 或玩法逻辑。

## 二、源码溯源矩阵

| 需求点 | 落实位置 |
|---|---|
| 稳定官方 FeatureId 与不可变定义产物 | `src/BetterUnturnedExperience.Plugin/OfficialFeatureRegistration.cs` |
| 与第三方相同的公开注册入口 | `BetterItemInteractionFeatureRegistration.Register()` → `BueRuntimeHost.Register()` |
| 可执行工厂与模块边界 | `OfficialFeatureRegistration.cs` 的 `ModuleFactory` / `Module` |
| Host 在 RegistrationOpen 阶段登记官方功能 | `BetterUnturnedExperiencePlugin.Awake()` |
| 官方/第三方平权与确定性 Catalog | `tests/BetterUnturnedExperience.Plugin.Tests/Program.cs` |

## 三、TDD 记录

1. **Red**：先在插件测试加入官方注册、Catalog 计数与排序断言；缺少实现时构建失败，`CS0103: BetterItemInteractionFeatureRegistration`。
2. **Green**：新增 `OfficialFeatureRegistration.cs`，使用公开 `BueRuntimeHost.Register()`，并提供定义 digest、工厂及最小模块；目标测试转绿。
3. **验证**：未进行超出本票范围的重构或原生功能接入。

## 四、编译验证

命令：

```powershell
dotnet msbuild tests\BetterUnturnedExperience.Plugin.Tests\BetterUnturnedExperience.Plugin.Tests.csproj /t:Build /p:Configuration=Release /v:minimal
dotnet msbuild BetterUnturnedExperience.sln /t:Build /p:Configuration=Release /v:minimal
```

结果：0 errors / 0 warnings。

## 五、测试结果

完整 7 个 Release 测试套件全部通过：

- DEV-05 ClientUi：PASS
- DEV-10 registration runtime：PASS
- DEV-06 network：PASS
- DEV-04 placement evaluator：PASS
- DEV-14 official registration parity：PASS
- DEV-08 runtime evidence package：PASS
- DEV-03 settings runtime：PASS

目标测试输出：`DEV-14 official registration parity tests: PASS`。

## 六、独立审计

### Standards

- 公开 Host Bridge 保持极窄，官方实现没有新增私有运行时 API。
- Contracts/Core/UI/LMN 边界未被新文件穿透；官方注册文件仅依赖 Contracts。
- 定义 payload 使用固定字节与预计算 SHA-256 digest，避免运行时动态描述或反射发现。

### Spec

- 官方 FeatureId 为稳定反向域名标识，并与显示语义分离。
- 官方条目通过 `BueRuntimeHost.Register` 与 No-op Fixture 共用入口。
- 注册仅发生在 `RegistrationOpen`；`CompleteRuntime()` 后仍由现有 Host barrier 拒绝晚注册。
- Catalog 中官方条目按 FeatureId 排在 No-op 之前，证明确定性排序。

审计结论：**PASS，无阻断项。**

## 七、产物与边界

staging 目录：`artifacts/DEV-14-official-registration-20260825/`

| 产物 | SHA-256 |
|---|---|
| `BetterUnturnedExperience.dll` | `A13695A1EF1CF99DC9EFDFA8698A1C79CA7D8CDE0A1C123C0F00FFE205E47B16` |
| `BetterUnturnedExperience.NoOpFixture.dll` | `CD177FE68CED6EAE4986062D74D9BC07555EB12B7D4E70B756FFC2AAED42FA02` |

本次构建更新了 BUE Host 源码，后续任何真实客户端运行证据必须重新绑定新 DLL 哈希；DEV-13 的旧运行证据不得外推到本次新产物。

本票不宣称 Better Item Interaction 玩法、原生库存权威链、ClientUi Satellite、SP、SteamP2PFriends P2P、U3DS 或三环境发布资格通过。
