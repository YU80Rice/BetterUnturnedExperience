# V3-R1 平台公开面现状盘点

- **Ticket**: V3-R1
- **Date**: 2026-09-09
- **Kind**: research（AFK 事实查证，不做决策）
- **Scope**: 现行 `src/` 代码 + 契约文档 + 测试工程 + `audit/` 结单；不改生产代码、不改契约、不改其他票据
- **六态标签**（可多标）: `已公开` / `内部可见` / `官方功能私有` / `生态可调用` / `已实现` / `静态存在但运行未证实`

## 0. 读法与证据纪律

- 锚点一律 `file:line`，相对仓库根。找不到事实写「未找到」，不推测。
- 「已公开」= 对第三方 IL 可见的 public 类型/成员（主程序集 `BetterUnturnedExperience` 或契约文档承诺面）。
- 「内部可见」= `internal` / `InternalsVisibleTo` 测试缝，第三方不可合法调用。
- 「官方功能私有」= 官方模块内部实现或官方专用接线，非生态 API。
- 「生态可调用」= 文档承诺 + public 入口，第三方 DLL 经公开桥可消费。
- 「已实现」= 源码存在可执行路径（非纯类型声明）。
- 「静态存在但运行未证实」= 类型/方法在源码或契约中存在，但宿主生产路径未接线，或 audit/测试未给出实机证据。判断依据：`audit/RELEASES.md` 结单范围 + 测试工程覆盖（`tests/`）。
- 运行时程序集身份：主 DLL `AssemblyName=BetterUnturnedExperience`（`src/BetterUnturnedExperience.Plugin/BetterUnturnedExperience.Plugin.csproj:5`）；Contracts/Core 源码以 `<Compile Include=... Link=Embedded*>` 嵌入主 DLL（同 csproj:11–31），运行时不携带独立 `BetterUnturnedExperience.Contracts.dll`。

## 1. 分类总表

| # | 盘点项 | 六态 | 主锚点 | 运行证实口径 |
|---|---|---|---|---|
| 1 | `BueRuntimeHost.Register` | 已公开 / 生态可调用 / 已实现 | `src/BetterUnturnedExperience.Plugin/BueRuntimeHost.cs:20-26` | 宿主测试（NoOpFixture 同桥）+ DEV-V2-24 改名实机「生态样板经公开桥 accepted=True」 |
| 2 | `BueNetworkApi` / `IFeatureBootstrap.Network` | 已公开 / 生态可调用 / 已实现 | `ContractTypes.cs:356-389`；`FeatureBootstrap.cs:42` | 宿主红测 + 官方 LIT/LIR/LHT 生产消费；生态独立 DLL 实机消费未找到 |
| 3 | `IFeatureBootstrap` 全面 | 已公开 / 生态可调用 / 已实现（Network/Events/OwnedEvents）+ 静态存在但运行未证实（Settings/Logger/Dependencies/Lifetime/Identity） | `ContractTypes.cs:96-111`；生产组合 `BueFeatureStartRuntime.cs:61-70` | 生产只填 Network/Events/OwnedEvents/LifecycleGeneration；其余传 null |
| 4 | 功能事件 OwnedEvents / TidyCompleted | 已公开 / 生态可调用 / 已实现 / 官方功能私有（发布侧） | `ContractTypes.cs:201-216`；发布 `InventoryTidyModule.cs:158-170` | 官方 LIT→LIR 宿主测试全链；生态订阅仅宿主测试用第三方 FeatureId，无独立生态 DLL 实机 |
| 5 | 宿主时钟 HostTick | 已公开 / 生态可调用 / 已实现 | `ContractTypes.cs:226-236`；泵 `BueRuntimeTickChain.Tick` `BueRuntimeCompletionChain.cs:202` | 宿主测试假时钟+生产泵；DEV-V2-24/25 三环境官方功能跑通间接证实泵在转 |
| 6 | Settings | 已公开（契约 DTO/IScopedFeatureSettings）/ 内部可见（面板编辑面）/ 官方功能私有（各模块 SettingsRuntime）/ 已实现 / 生态可调用（契约面）+ 静态存在但运行未证实（bootstrap.Settings） | `ContractTypes.cs:113-118`；`SettingsRuntime.cs:230` | 官方功能面板开关已实机；生态经 bootstrap.Settings 未接线 |
| 7 | Diagnostics | 已公开（契约 IFeatureLogger + 文档 BUE-PLATFORM-001）/ 内部可见（BueRuntimeLog、各 DiagnosticLogSink）/ 已实现 | `BueRuntimeLog.cs:12`；`BuePlatformDoubleInstallCheck.cs:81` | 001 红测六例 + DEV-V2-24 C4'' 实机正向双证；IFeatureLogger 生产未实现 |
| 8 | Failure isolation / CoreSafeMode | 已公开（枚举）/ 已实现（功能级 try/catch 隔离）/ 静态存在但运行未证实（CoreSafeMode 生产入口） | `ContractTypes.cs:12,80,133`；`FeatureRegistrationRuntime.cs:141-144` | 各适配器 isolated 标志有测试；`EnterCoreSafeMode` 生产调用未找到 |
| 9 | NoOpFixture | 已公开路径样板 / 生态可调用 / 已实现 | `src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs:7-29` | 宿主测试注册/晚注册拒绝；DEV-V2-24 改名实机 accepted=True |
| 10 | Contracts 程序集冻结面 | 已公开（类型）/ 内部可见（独立 DLL 产物）/ 已实现（嵌入主 DLL） | `src/BetterUnturnedExperience.Contracts/ContractTypes.cs` 全文；csproj AssemblyName | 独立 Contracts.dll 是 compile-time/test artifact，第三方运行时禁引用 |

---

## 2. `BueRuntimeHost.Register`（公开注册桥，SCR-GPT18-001）

**六态**: `已公开` / `生态可调用` / `已实现`

### 2.1 公开面

- `public static class BueRuntimeHost`，`Register(IFeatureRegistration)` 与 `Phase` 为 public；`Bind` / `Clear` / `CurrentRuntime` 为 `internal`（`src/BetterUnturnedExperience.Plugin/BueRuntimeHost.cs:7-41`）。
- 宿主未绑定时返回 `Accepted=false`、`Reason=HostUnavailable`、`DiagnosticId=BUE-HOST-001`（同文件:22-24）。
- 契约接口 `IBueFeatureRegistrationHost.Register`（`src/BetterUnturnedExperience.Contracts/ContractTypes.cs:54-57`）；实现 `FeatureRegistrationRuntime.Register`（`src/BetterUnturnedExperience.Core/Registration/FeatureRegistrationRuntime.cs:65-101`）。
- 准入：阶段门（HostStarting / CoreSafeMode / 非 RegistrationOpen）、定义校验、工厂非空、契约 Major=2 Minor≤0、ClientUi 可选校验、防重复 FeatureId。接受诊断 `BUE-REG-ACCEPT`（:100）；拒绝诊断 `BUE-REG-001`..`BUE-REG-009`（:69-90）。
- 官方功能走同一桥：`BetterItemInteractionFeatureRegistration.Register` 注释写明「deliberately uses the same public bridge as an external feature」（`src/BetterUnturnedExperience.Plugin/OfficialFeatureRegistration.cs:5-16`）。LIT/LIR/LHT/网络模块同样在插件 Awake 调 `BueRuntimeHost.Register`（`BetterUnturnedExperiencePlugin.cs:80-100`）。
- 文档承诺：`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:37`「注册桥公开且唯一……不存在官方私有通道」。

### 2.2 运行证实

- 宿主测试：NoOp 在 host 未绑定时 fail-closed；官方 + fixture 同桥接受；`CompleteRuntime` 后 catalog=2；晚注册 `PhaseClosed`（`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:337-347,440-441`）。
- 实机：SDK §8 补注第 1 项「生态样板经公开桥 `accepted=True`」（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:212`）；绑定候选 `a1b339bf…71359`（DEV-V2-24，已被 v8 取代，见 `audit/RELEASES.md:10,24`）。
- 未找到：仓库外真实第三方 DLL 经此桥注册的 audit 证据。

---

## 3. `BueNetworkApi` / `IFeatureBootstrap.Network`

**六态**: `已公开` / `生态可调用` / `已实现`（官方消费者已证实；生态独立 DLL 实机消费未找到）

### 3.1 契约面

- 命名空间 `BetterUnturnedExperience.Contracts.BueNetwork`（`ContractTypes.cs:283`）。
- `IBueNetworkApi` 成员：`RegisterChannel` / `UnregisterChannel` / `Subscribe(FeatureId, ChannelDirection, handler)` / `Sessions` / `SendToServer` / `SendToClients` / `SendToClient`（:356-389）。
- 配套：`NetworkSendResult`（含 `PartialFailure=204`，:288-298）、`ChannelDirection`（:351）、`IConnectionSession`（:334-345）、`ChannelRegistrationResult` / `ChannelVersionEntry`。
- `IFeatureBootstrap.Network` 注释冻结：永非 null、同实例贯穿模块寿命、不泄漏 Host/LMN/Unity 类型（`ContractTypes.cs:106-110`）。
- 文档登记：SDK §7 契约 2.0 条目 ①–④（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:154-177`）。

### 3.2 实现与组合

- 运行时实现 `BueNetworkRuntime : IBueNetworkApi`（`src/BetterUnturnedExperience.Core/Network/BueNetworkRuntime.cs:73`）。
- 功能面稳定门面 `DeferredBueNetworkApi : IBueNetworkApi`（`DeferredBueNetworkApi.cs:13`）——注册/订阅可在引擎角色未定时接受，稍后 replay；门面自身永不替换。
- 适配器：`NetworkModuleAdapter.FeatureNetworkApi` 注释「never null, never swapped」（`NetworkModuleAdapter.cs:161-167`）；内部 `NetworkApi` 指向 armed runtime，菜单期可为 null（:154-159）——该属性是适配器私有句柄，不在 `IBueNetworkApi` 上。
- 生产注入：`BueFeatureStartRuntime.StartCatalog` 把 `featureNetwork` 传入每个 `FeatureBootstrap`（`BueFeatureStartRuntime.cs:44,61-70`）；调用点 `BueRuntimeCompletionChain.TryStartRegisteredModulesCore`（`BueRuntimeCompletionChain.cs:108-118`），`featureNetwork == null` 则整批跳过启动。
- 组合 fail-fast：`FeatureBootstrap` 构造对 `network` 抛 `ArgumentNullException`（`FeatureBootstrap.cs:31`）。其余 bootstrap 成员按传入原样保留，可为 null（同文件:14-17 注释）。

### 3.3 官方消费者（官方功能私有接线，但是同一公开 API）

- LIT：`InventoryTidyModule.Start` 要求 `bootstrap.Network != null`，经 `LitTidyNetService` 注册频道（`InventoryTidyModule.cs:114-131`）。
- LIR：`InPlaceReloadModule.Start` 同要求，经 `LirRepackNetwork`（`InPlaceReloadModule.cs:100-115`）。
- LHT：`HordeTrackerModule` / `HordeTrackingModule` 持 `IBueNetworkApi`（`HordeTrackerModule.cs:25,79`）。

### 3.4 运行证实

- 契约形状测试：`tests/BetterUnturnedExperience.Contracts.Tests/Program.cs:74-89`。
- 注入红测：`AssertBueV2NetworkInjection`（`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs:3214-3239`）——null Network fail-fast；停用模块时同一实例返回空 Sessions / `NoSession`。
- 官方功能宿主全链：LIT 联机、LIR 纳入、LHT 纳入均走 `IBueNetworkApi`（同测试工程 `AssertBueV2LitMultiplayerPath` / `AssertBueV2LirAdoption` / `AssertBueV2LhtAdoption`，入口 :427-430）。
- 实机：`audit/RELEASES.md:24-25` DEV-V2-24/25 三环境官方四功能验收；未找到生态独立 DLL 作为 `IBueNetworkApi` 消费者的实机记录。

---

## 4. `IFeatureBootstrap` 全面

**六态**: `已公开` / `生态可调用`（接口） / `已实现`（子集） / `静态存在但运行未证实`（Settings / Logger / Dependencies / Lifetime / Identity）

接口定义（`src/BetterUnturnedExperience.Contracts/ContractTypes.cs:96-111`）：

| 成员 | 契约类型 | 生产 `BueFeatureStartRuntime.StartCatalog` 传入值（`BueFeatureStartRuntime.cs:61-70`） | 六态 |
|---|---|---|---|
| `Identity` | `FeatureScopeIdentity` | `default(FeatureScopeIdentity)`（空记录） | 已公开 / 静态存在但运行未证实（未填定义作用域） |
| `LifecycleGeneration` | `ulong` | `NextGeneration()` 宿主单调计数（:115-122） | 已公开 / 已实现 |
| `Settings` | `IScopedFeatureSettings` | `null` | 已公开 / 静态存在但运行未证实 |
| `Events` | `IFeatureEventSubscriber` | `bus.Subscriber(feature)` | 已公开 / 生态可调用 / 已实现 |
| `OwnedEvents` | `IOwnedFeatureEventPublisher` | `bus.Publisher(feature)` | 已公开 / 生态可调用 / 已实现 |
| `Logger` | `IFeatureLogger` | `null` | 已公开 / 静态存在但运行未证实 |
| `Dependencies` | `IDependencyCapabilityView` | `null` | 已公开 / 静态存在但运行未证实 |
| `Lifetime` | `IFeatureLifetime` | `null` | 已公开 / 静态存在但运行未证实 |
| `Network` | `IBueNetworkApi` | `featureNetwork`（永非 null，否则整批跳过） | 已公开 / 生态可调用 / 已实现 |

补充事实：

- 组合类 `FeatureBootstrap` 是 public 实现，但位于 Core 命名空间；因源码嵌入主 DLL，第三方 IL 能看到该具体类型（`FeatureBootstrap.cs:19`）。文档指引是「只引用公开 interface」（SDK §4:72），未承诺具体类稳定。
- `IFeatureLogger` 仅接口三方法 `Info/Warning/Error`（`ContractTypes.cs:121-126`）。全仓库 `rg ": IFeatureLogger"` **未找到实现类**。
- `IFeatureLifetime.TryTrack`、`IDependencyCapabilityView.Has/TryGet` 同文件:112,127-131；生产 Start 路径未接线。
- 测试构造同样把 Settings/Logger/Dependencies/Lifetime 传 null（`Program.cs:3225,3745,3981,5298` 等），即测试也未覆盖这些成员的生产语义。
- NoOp 模块 `Start` 返回 `default(FeatureStartResult)`（`NoOpFeaturePlugin.cs:47`）——`Started=false`，因此 **不会进入** `started` 集合（`BueFeatureStartRuntime.cs:78-82`），其 bootstrap 缝即使被调用也不被宿主追踪。

---

## 5. 功能事件（OwnedEvents / TidyCompleted / 宿主时钟发布）

**六态**: `已公开` / `生态可调用`（订阅） / `已实现` / `官方功能私有`（TidyCompleted 发布身份绑定 LIT FeatureId）

### 5.1 契约

- `IOwnedFeatureEventPublisher.TryPublish<TEvent>(string declaredEventId, TEvent value)`（`ContractTypes.cs:120`）。
- `IFeatureEventSubscriber.Subscribe<TEvent>(Action<TEvent> handler)`（:119）。
- `TidyCompleted` 只读 struct，`EventId = "io.github.yu80rice.bue.inventory-tidy/tidy-completed"`（:201-216）；载荷 Publisher / FirstPage / LastPage / Result / ConnectionGeneration / TransactionId。
- `TidyCompletionResult : byte { Succeeded=1, Rejected=2, Failed=3 }`（:200）。

### 5.2 总线实现（内部可见 composition）

- `FeatureEventBus` public 类（`src/BetterUnturnedExperience.Core/Events/FeatureEventBus.cs:38`），插件组合根 `BueHostEventRuntime` 为 `internal`（`BueHostEventRuntime.cs:16-17` 注释：第三方只经冻结契约接口绑定，保留宿主身份不可从外部铸造）。
- 发布身份规则：declaredEventId 必须以 `owner.Value + "/"` 为前缀且事件名非空，否则 `TryPublish=false` 并诊断（:97-106）。
- 宿主身份 `io.github.yu80rice.bue.host` 保留：`Publisher(owner)` 对它 `ArgumentException` fail-fast（:65-68）；时钟走 `TryPublishHost`（:115-125）。
- handler 锁外执行、单 handler 异常不扩散（:141-150）。
- 停止交接：`UnsubscribeAll(feature)` 在 `IFeatureModule.Stop` **返回后**由宿主调用（`BueFeatureStartRuntime.cs:94-112`）。

### 5.3 发布 / 消费

- 唯一生产发布路径：`InventoryTidyModule.PublishTidyCompleted` → `publisher.TryPublish(TidyCompleted.EventId, ...)`（`InventoryTidyModule.cs:158-170`）。本地执行器与联机服务都经 `PublishTidyCompletedForPage`（:185-189；`LocalTidyExecutor.cs:124`；`LitTidyNetService.cs:921`）。
- 官方消费：LIR `Events.Subscribe<TidyCompleted>(Consumer.Handle)`（`InPlaceReloadModule.cs:127`）；`TidyCompletedConsumer` 内部隔离异常（`TidyCompletedConsumer.cs:38-48`）。
- 生态同权：总线按事件 **类型** 派发，不按 FeatureId 过滤订阅者（`FeatureEventBus.Dispatch` :133-139 只匹配 `EventType`）。第三方只要 `Subscribe<TidyCompleted>` 即可收到。未找到生态 DLL 实机订阅证据。

### 5.4 运行证实

- 契约形状：`tests/BetterUnturnedExperience.Contracts.Tests/Program.cs:91-112`。
- 总线/同权：`AssertBueV2EventBusAndHostTick`（`Plugin.Tests/Program.cs:6088+`），含「功能不能发 HostTick」「生态与官方同总线订阅 TidyCompleted+HostTick」。
- LIR 纳入组含「事件消费 / 整理→压弹全链」（:5158-5168）。
- 实机：DEV-V2-24/25 用户验收「四功能无异常」含整理与换弹，但结单未单独点名 TidyCompleted 跨功能事件帧。生态订阅实机 **未找到**。

---

## 6. 宿主时钟（HostTick）

**六态**: `已公开` / `生态可调用` / `已实现`

### 6.1 契约与实现

- `HostTick` 只读 struct，`EventId = "io.github.yu80rice.bue.host/host-tick"`；载荷 TickNumber / DeltaTime / Phase=`TickPhase.Update=0`（`ContractTypes.cs:225-236`）。
- 生产者 `HostTickClock`（`src/BetterUnturnedExperience.Core/Events/HostTickClock.cs:32-87`）：`HostPublisherId = "io.github.yu80rice.bue.host"`（:35）；每 `Tick()` 恰一拍；序号从 1 起 +1；首拍 DeltaTime=0；回拨钳制为 0；`Tick()` 永不向泵抛出。
- 生产泵：`BueHostEventRuntime.EnsureCreated` 在插件 Awake（`BetterUnturnedExperiencePlugin.cs:70`）；每帧 `BueRuntimeTickChain.Tick` → `BueHostEventRuntime.TickOnce`（`BueRuntimeCompletionChain.cs:186-202`）。`BueHostEventRuntime` 为 internal。
- 官方订阅：LIR `Events.Subscribe<HostTick>(OnHostTick)`（`InPlaceReloadModule.cs:126`）；LHT 经 `OnHostTick`（测试驱动 `HordeTrackerModule`）。

### 6.2 运行证实

- 契约形状：`Contracts.Tests/Program.cs:114-129`。
- 假时钟组 + 生产 `TickOnce` 同权订阅：`Plugin.Tests/Program.cs:6190-6310`。
- LIR HostTick 双击驱动组（:5424-5428）；LHT tick 驱动（:8763-8764）。
- 实机：无单独「HostTick 帧计数」采集项。DEV-V2-24/25 官方功能在三环境可运行，间接表明 Update 泵链在转；生态 DLL 订阅 HostTick 的实机 **未找到**。

---

## 7. Settings（持久化与面板编辑面）

**六态**: `已公开`（契约 DTO + `IScopedFeatureSettings`） / `内部可见`（`IBueSettingsEditor`、面板模型） / `官方功能私有`（各官方模块自有 `SettingsRuntime`） / `已实现` / `生态可调用`（契约面可编译） / `静态存在但运行未证实`（`IFeatureBootstrap.Settings` 生产为 null）

### 7.1 契约面

- `IScopedFeatureSettings`：`GetSnapshot` / `TryGet` / `Submit`（`ContractTypes.cs:113-118`）。
- 配套 DTO：`SettingDescriptor` / `SettingValue` / `FeatureSettingsSnapshot` / `SettingChangeResult` / `ScopedSettingChangeRequest` / `SettingMutation` 等（:152-188）。
- CONTEXT「统一功能设置面板」：功能模块拥有校验、持久化与运行时权威，BUE 不替代这些事实源（`CONTEXT.md:37-39`）。

### 7.2 实现

- `SettingsRuntime : IScopedFeatureSettings` public（`src/BetterUnturnedExperience.Core/Settings/SettingsRuntime.cs:230-277`）。
- 持久化：`FileSettingsPersistence` 写 `{feature}.{scope}.bue-settings`，magic `BUE_SETTINGS_DOCUMENT_V1`（:92-109）；损坏文件隔离为 `.corrupt.*.bak`（:122-124）。`InMemorySettingsPersistence` 供测试（:39）。
- 官方模块各自 new `SettingsRuntime` + `FileSettingsPersistence`，**不经 bootstrap.Settings**：
  - LIT `InventoryTidyModule` 构造（`InventoryTidyModule.cs:45-54`）
  - LIR（`InPlaceReloadModule.cs:38,46`）
  - LHT（`HordeTrackerModule.cs:41,49`）
  - 网络模块 + V1 兼容（`NetworkModuleAdapter.cs:127-128`）
- 生产 Start 路径：`bootstrap` 的 Settings 参数为 null（`BueFeatureStartRuntime.cs:61-70`）。官方模块不读 `bootstrap.Settings`。

### 7.3 面板编辑面（内部可见）

- `IBueSettingsEditor` 为 `internal`（`src/BetterUnturnedExperience.ClientUi/ManagementPanel.cs:241-245`）。
- 路由：`SettingsRuntimeBueEditor` / `RoutingBueSettingsEditor`（`src/BetterUnturnedExperience.Plugin/NetworkSettingsEditor.cs:17,68`）。
- 组合：`ClientUiCompositionRoot` 把网络/LIT/LIR/LHT 的 `SettingsRuntime` 登记进路由表（`ClientUiCompositionRoot.cs:57-84`）。
- 未找到：生态功能设置自动出现在管理面板的登记缝（无「按 catalog 发现 SettingsRuntime」的通用路径）。生态若自建 `SettingsRuntime` 可编译，但面板不会自动挂载。

### 7.4 运行证实

- 官方开关面板：DEV-V2-24/25 实机验收含功能可用/可关（`audit/RELEASES.md:24-25`）。
- 宿主测试覆盖网络/LIT 等 `GetSnapshot`/`Submit` 路径（插件测试套件常跑）。
- 生态经 `IFeatureBootstrap.Settings` 读写：**未接线，运行未证实**。

---

## 8. Diagnostics（BueRuntimeLog / DiagnosticLogSink / BUE-PLATFORM-001）

**六态**: `已公开`（文档诊断 id + 契约 `IFeatureLogger`） / `内部可见`（日志与 sink） / `已实现`（001/002 与各适配器 sink） / `静态存在但运行未证实`（`IFeatureLogger` 生产实例）

### 8.1 `BueRuntimeLog`

- `internal static class BueRuntimeLog`（`src/BetterUnturnedExperience.Plugin/BueRuntimeLog.cs:12`）。
- 级别：`Runtime`=Debug（游戏内事件默认静音）、`Load`/`AnnounceReady`=Info、`Warn`=Warning（001 不受静默策略约束，:53-56）、`Error`/`ErrorFriendly` 永远打印（:67-87）。
- 测试缝 `Recorder`（:16）；生产 `Bind(ManualLogSource)`（:20-23），Awake 调用（`BetterUnturnedExperiencePlugin.cs:54`）。

### 8.2 `DiagnosticLogSink`

票面所列「DiagnosticLogSink」**不是**单一平台类型，而是多处 `internal static Action<string>` 测试/生产缝：

| 位置 | 锚点 | 用途 |
|---|---|---|
| `NetworkModuleAdapter.DiagnosticLogSink` | `NetworkModuleAdapter.cs:66,581-585` | 网络接管故障/生命周期；生产绑 `BueRuntimeLog` |
| `LmnV1CompatLayer.DiagnosticLogSink` | `LmnV1CompatLayer.cs:25` | V1 兼容层；生产绑 Runtime |
| `InventoryDragPreviewAdapter.DiagnosticLogSink` | `InventoryDragPreviewAdapter.cs:472` | 拖拽预览隔离 |
| `InventorySurfaceLifecycleAdapter.DiagnosticLogSink` | `InventorySurfaceLifecycleAdapter.cs:980` | 库存生命周期隔离 |
| `ClientUiCompositionRoot.DiagnosticSink` | `ClientUiTypes.cs:104`；插件绑 `BetterUnturnedExperiencePlugin.cs:138-146` | ClientUi 结构化行 |
| `FeatureEventBus` 构造 `diagnosticSink` | `FeatureEventBus.cs:42,49` | 事件拒绝/handler 错误；生产绑 `BueRuntimeLog.Runtime`（`BueHostEventRuntime.cs:34`） |

以上全部 `internal`，生态不可直接绑。契约替代面是 `IFeatureLogger`，但生产传入 null（见第 4 章）。

### 8.3 BUE-PLATFORM-001 防双装

- 决策核 `BuePlatformDoubleInstallCheck` `internal static`（`BuePlatformDoubleInstallCheck.cs:79-81`）；`DiagnosticId = "BUE-PLATFORM-001"`；扫描侧隔离 id `BUE-PLATFORM-002`（:89）。
- 入口：插件 Awake `RunPlatformDoubleInstallSelfCheck`（`BetterUnturnedExperiencePlugin.cs:66,231-242`）；扫描异常不阻塞启动。
- 日志：每冲突副本一条 Warning，字段 assembly / conflictLocation / selfPath / suggestion=「移除非官方副本」（:61-67,115）。
- 面板：冲突时 `SetDoubleInstallNotice`（`BetterUnturnedExperiencePlugin.cs:204-205`）；原生面板 `SetStatusWithPlatformNotice` 优先于其它状态行（`BueNativeManagementPanel.cs:904-911,910`）。
- 边界（代码注释与 SDK §6 一致）：只看已进 AppDomain 的程序集；不删文件；不替代 BepInEx GUID 去重。

### 8.4 运行证实

- 红测六例 `--bue-v2-platform-red`：无冲突 / 同名冲突 / 不同名 / 空路径 / 重复条目 / 诊断字段（`Plugin.Tests/Program.cs:326-329,9088-9137`）。
- 面板通知独立断言 `AssertPlatformPanelNotice`（:9358-9377）。
- 实机：SDK §8 补注（:208-216）+ `audit/RELEASES.md:24`。无冲突三环境零 001；标准 BepInEx 同名副本在类型扫描段身份折叠、到不了 Awake；C4'' 经 `Assembly.LoadFile` 非标准通道注入后逐条 001 Warning + 面板红行。001 实证口径 = 「非标准装载路径进来的副本的兜底网」。
- `IFeatureLogger` 生产实现：**未找到**。

---

## 9. Failure isolation / CoreSafeMode

**六态**: `已公开`（枚举与 CONTEXT 词汇） / `已实现`（多处功能级 try/catch 与 isolated 标志） / `静态存在但运行未证实`（`EnterCoreSafeMode` 生产调用；统一 FeatureState 机）

### 9.1 契约与词汇

- `FeatureRegistrationPhase.CoreSafeMode = 4`（`ContractTypes.cs:12`）。
- `FeatureStopReason.CoreSafeMode` / `RuntimeIsolated`（:80）。
- `FeatureState` 含 Isolating / Isolated（:133）。
- CONTEXT「功能级故障隔离」：默认只隔离该功能；核心或共享契约损坏才进全局核心安全降级（`CONTEXT.md:57-59`）。

### 9.2 已实现的隔离（非统一状态机）

生产是 **分散的 isolated 标志 + try/catch**，不是单一 `FeatureState` 推进器：

- 注册运行时：`EnterCoreSafeMode()` 把 Phase 设为 CoreSafeMode，随后 Register 返回 `CoreUnavailable`/`BUE-REG-002`（`FeatureRegistrationRuntime.cs:70,96,141-144`）。**全仓库 `EnterCoreSafeMode(` 调用点仅此定义，生产插件路径未找到调用。**
- 模块启动：工厂/Start 异常记日志并 skip，不崩溃宿主（`BueFeatureStartRuntime.cs:53-76`）；`Started=false` 不进入已启动集合。
- 网络模块：`NetworkModuleAdapter.isolated`；`Isolate` 停接管（`NetworkModuleAdapter.cs:112,477-495`）。
- 库存/拖拽适配器、运行时泵、ClientUi 组件槽（`ClientUiTypes.cs:86,184` catch 后记 IsolatedFeatureIds）、事件 handler、LIR consumer、LHT presentation。
- 双装自检自身：BUE-PLATFORM-002（`BetterUnturnedExperiencePlugin.cs:237-241`）。
- ClientUi `safeMode` 是 UI 组合根局部标志（`ClientUiTypes.cs:88-89,119`），与注册 Phase `CoreSafeMode` **不是同一条路径**。

### 9.3 运行证实

- 注册阶段机（含 CoreSafeMode 拒绝）有 Contracts.Tests / 早期 DEV-10 audit（`audit/2026-08-25/DEV-10-Independent-Audit-R2.md:33` 静态锁边界）。
- 各适配器 isolation 有宿主测试（拖拽/库存/网络/按钮注入本地隔离等）。
- 生产从未调用 `EnterCoreSafeMode`：事实为「方法存在、阶段拒绝逻辑存在、生产未接线」。全局核心安全降级 **运行未证实**。
- 未找到：生态模块故障被宿主按 `FeatureState.Isolated` 投影到面板的通用路径。

---

## 10. NoOpFixture（生态路径活样板）

**六态**: `已公开`（样板源码与文档指向） / `生态可调用`（演示路径） / `已实现`

### 10.1 源码

文件：`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`

- 独立 GUID `io.github.yu80rice.bue.noop`（:7）。
- `[BepInDependency("io.github.yu80rice.betterunturnedexperience", HardDependency)]`（:8）。
- Awake → `BueRuntimeHost.Register(new NoOpRegistration())`（:13-16,27-29）。
- `MinimumBueContract = (2,0)`（:35）；`ClientUi = null`（:37）。
- `NoOpModule.Start` 返回 `default(FeatureStartResult)`（:47）→ `Started=false`，不进宿主已启动集合，因此 **不消费** Network/Events/HostTick。它验证的是注册桥，不是平台服务全套。
- csproj：`AssemblyName=BetterUnturnedExperience.NoOpFixture`；`ProjectReference` 主插件且 `<Private>False</Private>`（`BetterUnturnedExperience.NoOpFixture.csproj:8,22`）——符合 CopyLocal=false / 禁捆绑。

### 10.2 文档与测试

- SDK §1 点名本文件为「生态路径的活样板」（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:26`）。
- 宿主测试：未绑 host fail-closed；同桥接受；晚注册 PhaseClosed（`Plugin.Tests/Program.cs:337-347,440-441`）。
- SDK 程序集身份：fixture AssemblyRef 必须是 `BetterUnturnedExperience`，禁止 `Contracts`/`Core`（:8532-8544）。
- 实机：DEV-V2-24 改名对照「生态样板经公开桥 accepted=True」（SDK §8:212）。

### 10.3 边界（事实，非评价）

- 样板 **不** 演示：Network 订阅/发送、HostTick、TidyCompleted 消费、Settings、Logger、面板条目。
- 未找到第二份仓库内生态样板 DLL（无 U3DS 安全样板、无带设置样板）。V3-T9 票面把「NoOpFixture 之外要不要第二个样板」列为待决（`.scratch/bue-v2-phase3-platform/issues/09-t9-sdk-contracts-split.md:16`）——本票不裁决。

---

## 11. Contracts 程序集冻结面清单

**六态**: `已公开`（嵌入主 DLL 的 public 类型） / `内部可见`（独立 `BetterUnturnedExperience.Contracts.dll` 构建产物） / `已实现`（源码嵌入）

### 11.1 部署形态

- 独立项目 `src/BetterUnturnedExperience.Contracts/BetterUnturnedExperience.Contracts.csproj`：`AssemblyName=BetterUnturnedExperience.Contracts`，只编译 `ContractTypes.cs`（csproj:8,28）。
- 主插件 **源码嵌入** 同一文件（`BetterUnturnedExperience.Plugin.csproj:11`），运行时类型落在 `BetterUnturnedExperience` 程序集。
- 宿主测试钉死：`typeof(FeatureId).Assembly == typeof(BueRuntimeHost).Assembly`；主程序集不得 AssemblyRef `BetterUnturnedExperience.Contracts` / `.Core`（`Plugin.Tests/Program.cs:8521-8530`）。
- SDK 明文：Contracts.dll「内部 compile-time/test artifact——不是玩家运行时依赖，不是第三方应携带的 ABI DLL」（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:74-79`）。

### 11.2 `ContractTypes.cs` 冻结面分组（单文件 390 行）

下列均为 `public`。行号相对 `src/BetterUnturnedExperience.Contracts/ContractTypes.cs`。

**身份 / 注册**

- `FeatureId` :6；`ContractVersion` :7；`FeatureDependency` :8；`Digest256` :9；`FeatureScopeIdentity` :10
- `FeatureRegistrationPhase` :12；`FeatureRegistrationReason` :13-18
- `FeatureDefinitionArtifact` :20-38
- `IFeatureModuleFactory` :40；`IClientUiSatelliteRegistration` :41-46；`IFeatureRegistration` :47-53
- `IBueFeatureRegistrationHost` :54-58；`FeatureRegistrationResult` :59-67

**生命周期 / Bootstrap**

- `FeaturePresentationState` / `FeaturePresentationView` :69-78
- `FeatureStopReason` :80；`FeatureStartResult` :86-93
- `IFeatureModule` :95；`IFeatureBootstrap` :96-111
- `IFeatureLifetime` :112；`IScopedFeatureSettings` :113-118
- `IFeatureEventSubscriber` :119；`IOwnedFeatureEventPublisher` :120；`IFeatureLogger` :121-126
- `IDependencyCapabilityView` :127-131
- `FeatureState` / `FeatureStatusView` :133-134
- `CoreRuntimeState` / `CoreRuntimeStatusView` / `CoreRuntimeStatusChangedEvent` :135-137

**物品交互 / 放置（BII 领域 DTO，随主 DLL 公开）**

- `ItemGridPosition` :139；`ContainerKind` / `ContainerReference` :140-141
- `ItemPlacementIntent` :142；`PlacementPreviewState` / `PlacementReason` :143-144
- `ItemPlacementPreview` :145；`IGridOccupancyView` :146
- `PlacementCandidateInput` :147；`IPlacementCandidateEvaluator` :148
- `DragInteractionState` / `DragInteractionView` :149-150

**设置**

- `SettingRevisionScope` / `SettingKind` / `SettingAuthority` :152-154
- `SettingValue` 及工厂方法 :155-169
- `SettingValueOption` / `SettingMutation` / `ScopedSettingChangeRequest` :170-172
- `SettingDescriptor` :173-178；`SettingPolicyView` :179；`SettingEntryView` :180
- `SettingSyncState` / `SettingSnapshotSource` :181-182
- `FeatureSettingsSnapshot` :183；`SettingChangeResult` :184
- `UpdateModuleConfigCommand` / `RequestModuleConfigSnapshotCommand` :185-186
- `ModuleConfigChangedEvent` / `ModuleConfigRejectedEvent` / `FeatureStatusChangedEvent` :187-189

**功能事件 / 宿主时钟**

- `TidyCompletionResult` / `TidyCompleted` :200-216
- `TickPhase` / `HostTick` :225-236

**错误码 / 能力协商（契约类型存在；生产握手路径是网络运行时内部，非本票逐项实机盘点）**

- `FrameworkErrorCode` :238-248
- `CapabilityDirection` / `CapabilityRequirement` / `CapabilityEnvironment` :249-251
- `CapabilityDescriptor` / `WireSemanticVersion` / `FeatureCapabilityManifest` :252-254
- `NegotiationState` / `NegotiatedFeatureView` :255-256
- `ConnectionHandshakeId` / `CapabilityHello` / `CapabilitySnapshot` / `CapabilityAck` :257-260
- `SessionReadyEvent` / `HandshakeReject` :261-262
- `SnapshotKind` / `SnapshotChunkEnvelope` :263-275

**BueNetwork（`namespace BetterUnturnedExperience.Contracts.BueNetwork` :283）**

- `NetworkSendResult` :288-298
- `ChannelRegistrationResult` :304-313；`ChannelVersionEntry` :316-321
- `IConnectionSession` :334-345
- `ChannelDirection` :351
- `IBueNetworkApi` :356-389

当前契约版本文档登记：**2.0**，宿主 `SupportedContractMajor = 2`（`FeatureRegistrationRuntime.cs:44`；SDK §7:152）。

### 11.3 可见性备注

- 放置/拖拽 DTO 与能力协商 DTO 随 Contracts 源码公开，因此第三方 IL **能引用**。文档「公开契约」列举的消费面是 Network / Bootstrap.Network / 功能事件 / 宿主时钟 / 设置 / 诊断（SDK §2:38），**未**把 `IPlacementCandidateEvaluator` 或握手 DTO 写成生态必用面。本票只记录「类型已公开存在」，不裁定是否应保持公开。
- `FeatureEventBus` / `HostTickClock` / `SettingsRuntime` / `FeatureBootstrap` / `FeatureRegistrationRuntime` 是 Core 的 public 类，因嵌入而出现在主 DLL。文档要求只依赖 interface。`InternalsVisibleTo` 仅测试程序集（`Plugin/Properties/AssemblyInfo.cs:3`）。

---

## 12. Contracts 拆分四条件——现行事实证据（喂 V3-T9）

原文（T7 决策 2，`.scratch/bue-v2-phase2-official-adoption/issues/07-developer-contract-double-install.md:29`）：

> 当前拆独立 SDK 程序集收益不足以抵消版本/分发/绑定复杂度。立项条件(出现才做):第三方需脱离完整 BUE DLL 编译 / 多仓库需稳定纯契约包 / runtime 与 SDK 发布节奏须独立 / 需公开桥接 adapter 而不暴露主程序集。

SDK 现行措辞同义（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md:88`）。愿景文档曾写「给第三方引用 `BetterUnturnedExperience.Contracts.dll`」（`.scratch/bue-v2-phase2-official-adoption/research/2026-09-07-bue-platform-vision-phase3.md:170`）；对账注记 2 已改为引用主 DLL + CopyLocal=false（同文件:217）。Phase-3 地图：本图不直接拆分 Contracts.dll，T9 只重评四条件（`.scratch/bue-v2-phase3-platform/map.md:25,43`）。

本票只列现行事实，**不判断四条件是否已触发**。

| # | 条件 | 现行事实 | 证据锚点 |
|---|---|---|---|
| ① | 第三方需脱离完整 BUE DLL 编译 | 现行指引与测试强制 **引用主 DLL**。NoOpFixture `ProjectReference` 主插件且 Private=false。宿主测试要求 fixture AssemblyRef `BetterUnturnedExperience`、禁止 Contracts/Core。未找到「无法或不愿引用完整主 DLL」的第三方作者陈述或 issue。 | SDK §4:59-72；`NoOpFixture.csproj:22`；`Plugin.Tests/Program.cs:8532-8544` |
| ② | 多仓库需稳定纯契约包 | 仓库内存在独立 `BetterUnturnedExperience.Contracts` 项目，但是 compile-time/test artifact，禁止作为运行时 ABI。未找到第二个消费 BUE 契约的外部 git 仓库、NuGet 包或「纯契约包」分发记录。DEV-12 审计曾指出独立 Contracts.dll 与单 DLL 嵌入并存、当时缺 SDK facade（`audit/2026-08-25/DEV-12-Independent-Audit-R1.md:20`）；后续政策定为嵌入+禁运行时引用。 | Contracts.csproj:8；SDK:74-79；DEV-12 audit |
| ③ | runtime 与 SDK 发布节奏须独立 | 当前对外部署物是单一 `BetterUnturnedExperience.dll`（`audit/RELEASES.md:8-10`，v8 `F7B7513C…`）。正式交付包含 DLL + 玩家手册 + SDK 文档（同表行 10）。SDK 文档随主仓库演进（SDK 文首:4）。未找到独立 SDK 版本号、独立发布通道或「runtime 已发但 SDK 未发」的记录。 | `audit/RELEASES.md:10,25`；SDK 文首 |
| ④ | 需公开桥接 adapter 而不暴露主程序集 | 现行公开桥 `BueRuntimeHost` **就在主程序集**（`BueRuntimeHost.cs:7`）。第三方必须引用主 DLL 才能调用 `Register`。未找到「只要 adapter、不要主程序集类型」的需求陈述。Core 具体类因嵌入也对引用主 DLL 的编译器可见——文档用「只引用 interface」约束，没有单独 adapter 程序集。 | `BueRuntimeHost.cs:7-20`；SDK §4:72,88 |

对照小结（事实句，非裁决）：四条件在仓库内均表现为「政策已写立项门、现行路径是主 DLL + 嵌入 Contracts」；**没有**找到已发生的触发实例（无外部作者要求纯契约包、无多仓库契约包、无独立 SDK 发版、无隐藏主程序集的 adapter 包）。

---

## 13. NoOpFixture 之外的第三方需求信号

检索范围：`.scratch/` 票据/结单/research、`audit/` 结单与独立审查、`docs/`。

### 13.1 找到的「第三方」表述——均为平台愿景/自身设计，不是外部作者来函

- CONTEXT「生态功能模块」定义（`CONTEXT.md:25-27`）与 V2 产品方向（:17-19）。
- T7 / DEV-V2-23：为「第三方开发者」写三段式契约（`.scratch/bue-v2-phase2-official-adoption/issues/07-developer-contract-double-install.md:10-12`；`DEV-V2-23-double-install-sdk-doc.md:11`）。
- 愿景对账注记 2：两层模型，生态=独立 DLL（`2026-09-07-bue-platform-vision-phase3.md:215-217`）。
- 架构期 DEV-11：「不实现真实第三方玩法」（`.scratch/better-unturned-experience-architecture/issues/DEV-11-noop-external-feature-host-bridge.md:25`）；audit 复述「未实现……真实第三方玩法」（`audit/2026-08-25/DEV-13-Independent-Audit-R1.md:68`）。
- DEV-10 review：「未宣称真实第三方 DLL」（`.scratch/better-unturned-experience-architecture/handoffs/DEV-10-Registration-Runtime-Review.md:29`）。
- LMN 生态盘点：GitHub 外部下游未找到；「全为作者家族」基于本地证据（`.scratch/bue-v2-lmn-adoption/research/V2-T8-v1-ecosystem-inventory.md:120`）。
- V3-T9 票面本身把「真实第三方需求」列为待本票输入（`issues/09-t9-sdk-contracts-split.md:20`）——说明决策前也未把该信号当已知。

### 13.2 未找到

- 未找到仓库外作者的 issue / PR / 邮件 / 用户原话要求接入 BUE 公开契约。
- 未找到第二份非 NoOp、非官方纳入（LIT/LIR/LHT 原为同一作者 Launch 系列）的生态 DLL。
- audit 结单中「SteamP2PFriends 第三方」字样指 **Unturned 原版网络层日志**，不是 BUE 生态功能（`audit/2026-09-05/DEV-V2-13/DEV-V2-13-closing-report.md:44`）。

**结论句**：NoOpFixture 是仓库内唯一活的生态路径样板；其外 **未找到** 真实第三方需求信号。存在的是 BUE 自己的平台愿景与官方纳入，不能记作外部需求。

---

## 14. 缺口与「运行未证实」汇总（供后续票对照，不作建议）

1. `IFeatureBootstrap.Settings` / `Logger` / `Dependencies` / `Lifetime` / 非 default 的 `Identity`：生产 Start 传 null 或 default；无实现类（Logger）或无接线。
2. `EnterCoreSafeMode`：仅定义 + 注册拒绝逻辑，生产未调用。
3. 生态设置进管理面板：无通用发现缝；面板路由写死官方 FeatureId。
4. 生态 DLL 实机消费 Network / HostTick / TidyCompleted：测试用第三方 FeatureId 证实总线同权；独立 BepInEx 生态 DLL 实机 **未找到**。NoOp 自身不 Start 成功，不练这些缝。
5. `IFeatureLogger` vs 内部 `BueRuntimeLog` / 多处 `DiagnosticLogSink`：诊断能力已实现但走内部缝，契约 Logger 未接线。
6. 独立 `BetterUnturnedExperience.Contracts.dll` 仍会由 Contracts 项目产出，政策禁止第三方运行时携带；与愿景旧段「引用 Contracts.dll」已由对账注记 2 覆盖。
7. 契约文件中的放置 DTO、能力协商 DTO、`CoreRuntimeStatus*` 公开存在；生产生态路径未把它们列为必用面，也无生态实机。

---

## 15. 主要证据源

- 代码：`src/BetterUnturnedExperience.Plugin/BueRuntimeHost.cs`、`BueFeatureStartRuntime.cs`、`BueHostEventRuntime.cs`、`BueRuntimeLog.cs`、`BuePlatformDoubleInstallCheck.cs`、`BueRuntimeCompletionChain.cs`、`BetterUnturnedExperiencePlugin.cs`、`OfficialFeatureRegistration.cs`、`NetworkModuleAdapter.cs`
- 契约：`src/BetterUnturnedExperience.Contracts/ContractTypes.cs`
- 组合：`src/BetterUnturnedExperience.Core/Registration/FeatureBootstrap.cs`、`FeatureRegistrationRuntime.cs`、`Events/FeatureEventBus.cs`、`Events/HostTickClock.cs`、`Settings/SettingsRuntime.cs`、`Network/BueNetworkRuntime.cs`、`Network/DeferredBueNetworkApi.cs`
- 官方功能：`src/BetterUnturnedExperience.Lit/InventoryTidyModule.cs`、`src/BetterUnturnedExperience.Lir/InPlaceReloadModule.cs`、`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`
- 文档：`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`、`CONTEXT.md`、`audit/RELEASES.md`
- 测试：`tests/BetterUnturnedExperience.Plugin.Tests/Program.cs`、`tests/BetterUnturnedExperience.Contracts.Tests/Program.cs`
- 决策原文：`.scratch/bue-v2-phase2-official-adoption/issues/07-developer-contract-double-install.md:29`

