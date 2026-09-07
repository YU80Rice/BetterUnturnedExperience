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
- **③ 发送结果 + `PartialFailure`（2026-09-06，DEV-V2-16）**：`NetworkSendResult`
  新增枚举成员 `PartialFailure = 204`；发送语义冻结为**会话驱动组播**：
  - `SendToClients` = 向当前**已建立（established）** BUE 会话**逐一定向**发送（每会话一帧，target = 该会话 peer steam id），不是无目标帧交底层广播；未装 BUE 的原版玩家不收，本地主机身份天然不在远端会话集合。
  - 发送结果冻结：established 快照空 → `NoSession`；全部目标送达 → `Sent`；有目标且全部失败 → `LocalTransportUnavailable`；部分送达部分失败 → `PartialFailure`。发送**不持状态锁**。
  - `SendToClient` 维持按 `IConnectionSession` 寻址，**不新增** SteamId 重载；校验会话**归属本运行时**（按对象身份，非 id 相等——外来/伪造会话对象即持活代际 id 亦拒）、**established**、**当前连接代际**（已丢弃/被替换的会话对象拒）。
  - 频道未注册仍优先返回 `ChannelNotRegistered`；超限载荷保留专用 `PayloadTooLarge` 结果（一次性预检，不并入逐目标传输失败聚合）。
- **④ `Sessions` 收窄 established（2026-09-06，DEV-V2-16）**：`IBueNetworkApi.Sessions`
  只返回**已建立会话快照**；pending（握手未完成）会话仅运行时内部可见。破坏性来源：依赖 pending 会话可见性的行为不再受支持——pending 本就是内部握手状态，从未被承诺为公开语义。
- **⑤ 功能事件 `TidyCompleted`（2026-09-07，DEV-V2-19）**：只读 struct 入 Contracts 冻结面——跨功能协作的唯一公开缝，官方与生态功能同权消费（生态订阅同一事件，无需 Harmony postfix 挂进官方功能）。冻结面：
  - 事件身份串 `TidyCompleted.EventId = "io.github.yu80rice.bue.inventory-tidy/tidy-completed"`，由发布者 FeatureId 派生（派生规则：`<发布者 FeatureId>/<事件名>`）。
  - 载荷：`Publisher`（发布者 FeatureId）、`FirstPage`/`LastPage`（整理对象范围=连续页区间含端点）、`Result`（`TidyCompletionResult : byte { Succeeded = 1, Rejected = 2, Failed = 3 }`）、`ConnectionGeneration`（连接代际，0 = 不适用——本地/单人路径；真实代际从 1 起）、`TransactionId`（功能私有事务/操作标识）。
  - 发布/订阅经宿主事件总线（`IOwnedFeatureEventPublisher` / `IFeatureEventSubscriber`）：发布者只能发布身份串由**自己** FeatureId 派生的事件（外来身份串 `TryPublish=false`）；handler 锁外执行、单 handler 异常不扩散且浮出结构化诊断；订阅句柄独立幂等；功能停止由宿主注销其全部订阅（`FeatureEventBus.UnsubscribeAll(feature)` 为宿主停用边界交接缝——宿主模块 Start/Stop 路径在 `IFeatureModule.Stop` 返回后调用，随 DEV-V2-21/22 落地）。
- **⑥ 宿主时钟 `HostTick`（2026-09-07，DEV-V2-19）**：只读 struct 入 Contracts 冻结面——功能模块唯一的帧级驱动缝，**功能模块不得自建 Unity Update 泵**。冻结面：
  - 事件身份串 `HostTick.EventId = "io.github.yu80rice.bue.host/host-tick"`，由平台宿主标识 `io.github.yu80rice.bue.host`（宿主专用，非注册功能）派生；时钟由宿主统一产生——宿主标识为总线**保留身份**（`Publisher(宿主标识)` 参数异常 fail-fast，宿主时钟经总线内部宿主路径发布），任何功能都无法伪造宿主时钟。
  - 载荷只含时间与序号、不携带功能逻辑：`TickNumber`（单调 ulong，从 1 起严格 +1）、`DeltaTime`（float 秒，相邻 tick 单调时差，首 tick 0）、`Phase`（`TickPhase : byte { Update = 0 }`，阶段与频率固定=每宿主 Update 节拍恰一 tick；新增阶段属冻结面变更须登记）。
  - 冻结不变性：回调主线程执行（生产单驱动=插件 Update 泵链，时钟自身不建线程）；功能停止自动注销其时钟订阅；单订阅者异常不扩散（诊断浮出，不静默吞）；`Tick()` 永不向泵抛出。

- **⑦ `FeatureStartResult` 可构造结果（2026-09-07，DEV-V2-21，加性变更不升 Major）**：只读 struct 新增公开构造器
  `FeatureStartResult(bool started, FrameworkErrorCode error, string diagnosticId)`。
  - 加性理由：仅新增构造器，既有 getter 与 `default(FeatureStartResult)` 语义不变，旧引用无需重编译对齐，无破坏面——按本节规则（破坏性变更才升 Major）维持 2.0。
  - 冻结语义：宿主模块启动路径（`IFeatureModule.Start(IFeatureBootstrap)`，DEV-V2-21 落地）以显式结果回报启动结局——`Started=false`/`Error`/`DiagnosticId` 为显式失败回报，不再是隐式默认值；启动失败的模块不进入宿主已启动集合（宿主停止交接 `UnsubscribeAll` 只作用于已启动功能）。

> 后续票逐条追加。
