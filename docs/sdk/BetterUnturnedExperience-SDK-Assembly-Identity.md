# BUE SDK 程序集身份规则（DEV-12）

## 唯一策略

第三方功能的编译期引用与运行期引用都必须指向：

```text
BetterUnturnedExperience.dll
AssemblyName: BetterUnturnedExperience
Version: 0.0.0.0（当前预发布构建）
```

第三方 DLL 的 IL 必须将 `FeatureId`、`IFeatureRegistration`、`IFeatureModule`、Settings DTO、Presentation DTO 及 `BueRuntimeHost` 解析为 `[BetterUnturnedExperience]` 类型。

## Contracts 项目边界

`src/BetterUnturnedExperience.Contracts` 仍保留用于 Contracts 隔离单元测试、源码审查和内部编译验证；它生成的 `BetterUnturnedExperience.Contracts.dll` 是**内部 compile-time/test artifact**，不是玩家运行时依赖，也不是第三方插件发布时应携带的 ABI DLL。

任何第三方运行时 DLL 若出现以下 AssemblyRef，均视为 DEV-12 依赖闭包失败：

```text
BetterUnturnedExperience.Contracts
BetterUnturnedExperience.Core
```

## 编译建议

第三方功能项目直接引用 BUE 主 DLL：

```xml
<Reference Include="BetterUnturnedExperience">
  <HintPath>path\to\BetterUnturnedExperience.dll</HintPath>
  <Private>False</Private>
</Reference>
```

运行时部署只需：

```text
BepInEx\plugins\BetterUnturnedExperience.dll
BepInEx\plugins\ThirdPartyFeature.dll
```

不得把 `BetterUnturnedExperience.Contracts.dll` 或 `BetterUnturnedExperience.Core.dll` 作为 BUE 单 DLL 部署的一部分复制到插件目录。
