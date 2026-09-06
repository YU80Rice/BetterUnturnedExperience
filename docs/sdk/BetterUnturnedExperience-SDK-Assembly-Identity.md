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

## 契约版本演化（DEV-V2-14 起）

公开契约按契约版本（`ContractVersion`）演化：冻结面的**破坏性变更升 Major**，随每次升级在本节登记变更条目；功能注册的 `MinimumBueContract` 与宿主注册门槛（当前支持契约 Major = 2）对齐新版本。第三方开发者按本节对照升级。

**当前契约版本：2.0**（宿主注册门槛 `SupportedContractMajor = 2`）。

### 2.0（2026-09-06，DEV-V2-14，破坏性升 Major）

- **① 入站订阅与方向**：`IBueNetworkApi` 新增
  `IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler)`
  与 `enum ChannelDirection : byte { FromClients = 0, FromServer = 1 }`。冻结语义：
  - 方向 = **入站帧来源**（来自客户端 / 来自服务器），不是本地角色；同一频道可分别订阅两方向。
  - 每次订阅返回**独立幂等的句柄**，`Dispose` 只注销自己的委托（同一委托订阅两次各得一份派发；重复 `Dispose` 安全）。
  - handler 表与频道注册**解耦**：订阅未注册频道合法；帧仅在接收侧已注册频道且流量到达后派发。
  - handler 在状态锁外执行；单个 handler 异常不扩散到其它订阅、不冲击传输泵。
  - 空 handler（`ArgumentNullException`）与未定义方向值（`ArgumentOutOfRangeException`）属开发者错误，参数异常 fail-fast。
  - 破坏性来源：`IBueNetworkApi` 接口新增成员，旧实现/旧引用需重编译对齐。
- **② 功能获取网络入口**：`IFeatureBootstrap` 新增 `BueNetwork.IBueNetworkApi Network { get; }`。冻结要求：
  - `Network` **永非 null**（宿主组合点 fail-fast）。
  - 网络模块停用/未就绪时方法返回**显式结果**：`RegisterChannel` / `UnregisterChannel` / `Subscribe` 正常工作，`Sessions` 为空快照，发送按既有枚举返回（空快照 → `NoSession`），生命周期事件不触发，不新增专用查询面。
  - 功能停止后订阅句柄失效且可安全重复释放。
  - 属性类型为纯 C# 契约接口，不泄漏 Host / LMN / Unity 类型。

> 后续票逐条追加（③ 发送结果 + `PartialFailure`；④ `Sessions` 收窄 established；⑤ 版本登记流程本身）。
