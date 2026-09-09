# BUE 开发者契约与 SDK 引用（程序集身份 · 防双装 · 契约版本）

> 面向第三方生态功能开发者。DEV-12 建立程序集身份规则，DEV-V2-23 按冻结八节大纲扩写为完整开发者契约。
> 本文是 BUE 第三方契约的**唯一事实源**（不设第二文档目录）；措辞与运行时实现（`BueRuntimeHost` / `BuePlatformDoubleInstallCheck`）同仓库演进。

## 1. 适用范围

**读这份文档的人**：想把功能接入 BUE 运行时的第三方开发者（生态功能作者）。

BUE 的功能分两层——本文只约束第二层：

1. **官方功能（第一层）**：更好的物品交互（BII）、背包整理（LIT）、更好的换弹体验（LIR）、更好的尸潮播报（LHT）都是 BUE 官方维护的**源码模块**，构建期聚合进唯一的主 DLL。玩家安装列表里官方文件永远只有一个：`BetterUnturnedExperience.dll`。它们不是四个独立插件，不作为分发单元存在。
2. **生态功能（第二层）**：你（第三方作者）交付的**独立 DLL**，作为普通 BepInEx 插件安装。发现、扫描、实例化全部由 BepInEx 原生承担——BUE 不自建模块加载器、不扫描你的 DLL、不决定你的加载顺序。BUE 扮演的角色是**你 DLL 的前置库与运行时平台**。

一个生态 DLL 的完整生命周期（后文逐一展开）：

```text
独立 BepInEx 插件项目
  → [BepInDependency] 声明 BUE 前置（GUID 见 §2）
  → 编译期引用主 DLL，CopyLocal=false，禁止捆绑（§4）
  → 运行期经公开注册桥 BueRuntimeHost.Register（SCR-GPT18-001）接入（§4）
  → 消费平台服务：BueNetworkApi / IFeatureBootstrap.Network / 功能事件（TidyCompleted）
    / 宿主时钟（HostTick）/ 设置 / 诊断 / 隔离（§2、§4）
```

生态路径的活样板：`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`（独立 GUID + HardDependency 前置 + 经公开桥注册，仓库内长期在跑、被宿主测试验证）。

**本文范围**：身份冻结面、编译期引用、双装处置、契约版本演化、实机验证清单。具体功能怎么写（网络协议设计、UI 适配等）不在本文。

## 2. 承诺

BUE 对第三方生态功能承诺以下冻结面：

- **插件 GUID 冻结**：`io.github.yu80rice.betterunturnedexperience`。你的 `[BepInDependency]` 绑定这个 GUID，它不会变。
- **程序集名冻结**：`BetterUnturnedExperience`（`AssemblyName`）。你的 IL 中的类型引用解析到这个程序集身份。**未来若要改程序集名，不承诺兼容——必须作为破坏性公告和迁移事件处理**（不存在"既冻结又不承诺"的歧义地带）。
- **公开契约按契约版本演化**：冻结面的破坏性变更必升 Major 并登记（§7）；非破坏性加性变更维持版本号、随条目登记。你的功能注册携带 `MinimumBueContract`，宿主按契约版本门槛准入。
- **注册桥公开且唯一**：`BueRuntimeHost.Register(IFeatureRegistration)`（SCR-GPT18-001）是官方功能与生态功能**同一个**入口——不存在官方私有通道。注册返回明确的接受或拒绝结果（含 FeatureId 校验、防重复注册、契约版本门槛）。
- **平台服务同权**：生态功能与官方功能使用完全相同的网络（`IBueNetworkApi` / `IFeatureBootstrap.Network`）、宿主时钟（`HostTick`）、功能事件（`TidyCompleted`）、设置与诊断契约。
- **文件名不是契约身份**（精确措辞见 §5）：受支持的 BepInEx 加载方式下，第三方绑定依赖的是插件 GUID 与程序集身份，不是 DLL 文件名。

## 3. 不承诺

以下各项**不在** BUE 的承诺范围内，第三方不应依赖：

- **非 BepInEx 加载方式**：BUE 只承诺受支持的 BepInEx 加载路径下的行为。
- **未验证的 Preloader `AssemblyResolve` 等内部行为**：Doorstop/Preloader 层的解析细节不是契约面。
- **任意修改程序集名后仍兼容**：改 `AssemblyName` = 破坏性事件（§2），不做兼容兜底。
- **任意重命名 / 复制 / 阴影加载后的行为**：把 BUE DLL 改名、复制到别处、经阴影副本加载——这些脱离受支持部署前提（§5），行为不承诺。
- **把单次 Mono/.NET 加载实验当永久 ABI 保证**：一次性加载观察不构成契约。
- **防双装检测的完备性**：`BUE-PLATFORM-001` 自检（§6）有明确边界——它不是完整防重复加载系统，不替代 BepInEx GUID 去重，不保证捕获未加载 / 加载失败 / 隔离上下文中的副本。
- **替你处置文件**：BUE 永不自动删除或移动用户文件；双装的处置动作永远留给用户（§6）。

## 4. 编译期引用指引

**生态 DLL 的接入六步**（对应样板 `NoOpFeaturePlugin.cs`）：

1. **独立 BepInEx 插件项目**：你交付的是独立 DLL，带自己的 `[BepInPlugin]` GUID——不得复用 BUE 的 GUID，也不得把功能源码提交进 BUE 仓库请求聚合（官方功能是源码模块，生态功能是独立 DLL，两层不混）。
2. **声明前置**：`[BepInDependency("io.github.yu80rice.betterunturnedexperience", BepInDependency.DependencyFlags.HardDependency)]`。硬依赖保证 BepInEx 以 GUID 拓扑序先加载 BUE；BUE 缺席时你的插件不会被启动（fail-closed）。
3. **引用主 DLL**：只引用官方主 DLL，`CopyLocal=false`：

```xml
<Reference Include="BetterUnturnedExperience">
  <HintPath>path\to\BetterUnturnedExperience.dll</HintPath>
  <Private>False</Private>
</Reference>
```

4. **禁止捆绑**：不得把 `BetterUnturnedExperience.dll` 捆进你的发布包（`Private=False` 即编译期落实）。捆绑是双装的主因——你的发布目录里多出一份 BUE DLL，玩家一旦按目录部署就会制造同名副本，触发 §6 诊断甚至运行时冲突。**发布包里只有你自己的 DLL。**
5. **经公开桥注册**：在你的 Awake（或初始化路径）调用 `BueRuntimeHost.Register(你的 IFeatureRegistration)`（SCR-GPT18-001），检查返回结果的 `Accepted` / `Reason` / `DiagnosticId`——被拒绝就显式降级，不要假设注册必成。注册携带 `MinimumBueContract`，**与目标 BUE 版本的契约版本对齐**（§7）。
6. **消费平台服务**：从 `IFeatureBootstrap.Network` 拿网络 API（永非 null）、订阅 `HostTick` 与功能事件、走设置与诊断缝。生命周期规则：`IFeatureModule.Start/Stop` 驱动安装与撤销，停止后宿主注销你的订阅——不要自建 Unity Update 泵，不要自建线程泵。

**只引用公开 interface，不复制 implementation**：你的 IL 应只解析 `BetterUnturnedExperience` 主程序集里的公开契约类型（`FeatureId`、`IFeatureRegistration`、`IFeatureModule`、Settings/Presentation DTO、`BueRuntimeHost` 等）。

**Contracts 项目边界**（DEV-12 延续）：`BetterUnturnedExperience.Contracts.dll` 是**内部 compile-time/test artifact**——不是玩家运行时依赖，不是第三方应携带的 ABI DLL。任何第三方运行时 DLL 的 AssemblyRef 出现以下程序集名，即视为依赖闭包失败：

```text
BetterUnturnedExperience.Contracts
BetterUnturnedExperience.Core
```

**运行时部署形态**（你的安装说明应这样写）：

```text
BepInEx\plugins\BetterUnturnedExperience.dll    ← 唯一官方文件（玩家另行安装）
BepInEx\plugins\YourEcoFeature.dll             ← 你的 DLL，只含你自己
```

**独立 SDK 程序集拆分——暂缓**：当前不拆独立 SDK DLL（引用面 = 主 DLL + `CopyLocal=false`）。出现以下四条件之一才重新立项：① 第三方需脱离完整 BUE DLL 编译；② 多仓库需要稳定的纯契约包；③ 发布节奏须独立；④ 需要公开桥接 adapter 而不暴露主程序集。此前拆分只会提前多一个 adapter 和发布事实源。

## 5. GUID · 程序集名 · DLL 文件名 FAQ

**Q：BepInEx 按什么顺序加载插件？跟文件名有关吗？**
前置解析按**插件 GUID**（依赖拓扑 + 硬依赖检查），加载序 = GUID 拓扑序——与 DLL 文件名无关。"按文件名序加载"是讹传。

**Q：运行时我的类型引用按什么解析？**
按**程序集身份**（程序集名 + 版本），同样与文件名无关。这就是为什么程序集名冻结（§2）而文件名不是契约身份。

**Q：改 DLL 文件名安全吗？**
受支持的 BepInEx 加载方式下，改文件名不影响 GUID 解析与类型绑定。**用户期待已登记：未来版本更新可变的是文件名，程序集名不动**——动了前置引用就断。但文件路径、加载目录与 BepInEx 发现规则本身属**部署前提**（§3）：把 DLL 挪到 BepInEx 扫描不到的地方，BepInEx 根本不会加载它，这不在任何契约讨论范围内。

**Q：plugins 目录里出现两个同 GUID 的 BUE DLL？**
这是 **BepInEx 原生行为**的管辖范围：BepInEx 去重（留一、跳一、单实例），不报 fatal。BUE 不重复处理这个场景——玩家按 FAQ 删掉多余副本即可。

**Q：两个 DLL 同程序集名但 GUID 不同呢？**
BepInEx 不拦这个（它只去重 GUID）。同名副本会进入类型绑定冲突域——这是 **BUE 自检补强**的场景：`BUE-PLATFORM-001`（§6）在 BUE 启动时扫描已加载程序集并报告冲突副本。

**Q：BUE 未来想改程序集名怎么办？**
按 §2：程序集名若要改 = 破坏性公告 + 迁移事件处理，BUE 提前公告并给出迁移窗口，不承诺旧引用继续工作。

## 6. 双装诊断 BUE-PLATFORM-001

**机制**：BUE 启动（Awake）注入式自检——取**已进入 AppDomain 的程序集列表**，检查与 BUE 同程序集名（`BetterUnturnedExperience`，大小写不敏感的保守口径）的冲突副本。决策核只消费程序集名 / 路径 / 是否自身的视图记录，**不触文件系统、无任何文件删除路径**。

**诊断内容**（每冲突副本一条日志行，Warning 级）：

```text
BUE double-install detected diagnosticId=BUE-PLATFORM-001 assembly=BetterUnturnedExperience
conflictLocation=<冲突副本路径> selfPath=<当前 BUE 路径> suggestion=移除非官方副本
```

字段至少含：检测到的程序集名（`assembly`）、冲突副本 Location（`conflictLocation`）、当前 BUE 路径（`selfPath`）、「移除非官方副本」建议（`suggestion`）。位置无法读取的副本以 `(路径不可用)` 占位，仍被报告；重复列举的同一路径去重为一条。

**可见位置**：

- **日志**：Warning 级结构化行（不受运行时静默策略约束），BepInEx `LogOutput.log` 可见。
- **管理面板**：检出冲突时，面板底部红色状态行显示「错误：检测到 BetterUnturnedExperience 冲突副本（BUE-PLATFORM-001）：请移除非官方副本后重启游戏，详见 BUE 日志。」（「错误：」前缀由面板红色状态通道统一附加；双装通知优先于兼容性通知，且在面板目录为空 / 未选中条目的界面分支同样可见）。

**边界（措辞冻结，同样是不承诺的一部分）**：

- 只处理**已进入 AppDomain** 的程序集；不保证捕获未加载、加载失败、隔离上下文中的副本。
- **不替代 BepInEx 的 GUID 去重**（同 GUID 双装走 §5 FAQ 的 BepInEx 原生行为）。
- **不宣传为完整防重复加载系统**——它是诊断补强，不是加载器。
- **不自动删除用户文件**：BUE 永不删、不移动、不改名任何副本；处置动作完全留给用户。

**用户处置步骤**：按日志/面板给出的 `conflictLocation` 定位副本 → 确认它不是官方部署物（官方位置见 `selfPath` 或 §4 部署形态）→ 手动移除非官方副本 → 重启游戏。诊断在副本移除后不再出现。

**分工边界**（谁管哪种双装）：

| 场景 | 管辖方 | 行为 |
|---|---|---|
| 同 GUID 双装 | BepInEx 原生 | 留一跳一、单实例、不报 fatal + 本文 §5 FAQ |
| 同程序集名、不同 GUID | BUE 自检补强 | `BUE-PLATFORM-001` 结构化诊断（日志 + 面板） |

**自检自身的隔离**：扫描侧故障（个别程序集元数据不可读、注入源异常）以 Warning 行留痕并隔离——自检永远不阻塞 BUE 启动、不影响其余程序集的扫描；`BUE-PLATFORM-001` 专指双装冲突诊断，不挪用。

**验收锚**：红测锚 `--bue-v2-platform-red`（`tests/BetterUnturnedExperience.Plugin.Tests`，票面冻结六例收集式：无冲突 / 同程序集名冲突 / 不同程序集名 / 空路径 / 重复条目 / 诊断 id 与关键字段——程序集列表注入 seam，不触文件系统）；面板通知面另有独立断言随套件常跑（`AssertPlatformPanelNotice`）。实机验证清单见 §8。

## 7. 契约版本演化

公开契约按契约版本（`ContractVersion`）演化：冻结面的**破坏性变更升 Major**，随每次升级在本节登记变更条目；功能注册的 `MinimumBueContract` 与宿主注册门槛（当前支持契约 Major = 2）对齐新版本。第三方开发者按本节对照升级（编译期指引见 §4 第 5 步）。

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

## 8. 实机验证清单（DEV-V2-24 执行）

本节列出契约面的实机验证项——**红测不能替代实机结论**，由终票 DEV-V2-24 在三环境实机验收时执行并归档证据：

1. **改名实机对照**：重命名 BUE DLL 文件名后，BepInEx 仍按 GUID 拓扑序加载、第三方类型绑定不受影响（§5「文件名不是契约身份」的实证）。
2. **Mono `LoadFile` 二次探测**：Mono 运行时对同一程序集的二次 `LoadFile` 行为观察——记录事实，不据此扩大承诺（§3）。
3. **同版本程序集最终谁保留**：同名同版本程序集在加载冲突时的最终保留方观察——记录事实，不据此扩大承诺。
4. **不同 GUID + 同程序集名（红测 + 实机双证）**：构造独立 GUID 的同名副本部署，实机确认 `BUE-PLATFORM-001` 诊断行（日志 + 面板）与 §6 边界行为。红测锚 `--bue-v2-platform-red` 已钉决策核，但**红测不能替代实机结论**。
5. **Preloader `AssemblyResolve`**：Preloader 层解析行为观察——只记录，不承诺（§3）。

附：防双装自检的真机基线——**无冲突部署**（裸 BUE 单 DLL）启动日志零 `BUE-PLATFORM-001` 行；上述第 4 项的冲突部署出现 Warning 诊断行与面板红色状态行。

**证据纪律**：实机记录统一绑定候选身份（`assembly-identity sha256` 锚行与 RELEASES 候选哈希一致），沿 `audit/RELEASES.md` 的门禁与批准链。

### §8 实机执行结果补注（2026-09-09，DEV-V2-24 采集，候选 `a1b339bf…71359`）

原文保留不改写，以下为实机事实：

- **第 1 项（改名对照）✓**：`BetterUnturnedExperience.r24.dll` 加载成功，`assembly-identity path=…r24.dll sha256=候选原值`，生态样板经公开桥 `accepted=True`，零 001——GUID 前置解析与 IL 程序集名绑定均与文件名无关实证。
- **第 2/3 项（LoadFile 二次探测/同版本谁保留）事实**：BepInEx 类型扫描段（先于一切插件 Awake）即发生 Mono LoadFrom **身份折叠**——同程序集名副本被 `Skipping over type … as no metadata attribute is specified` 跳过，`[PROBE] awake` 零条，**副本程序集从未进入 AppDomain**；类型绑定始终由官方副本承担；Preloader 零新增解析异常。
- **第 4 项（不同 GUID + 同程序集名）**：红测半 CLEAN 维持（`--bue-v2-platform-red`，真实 AppDomain 注入两副本验证决策核）；**实机正向双证经修正仪器完整取得（C4''，2026-09-09）**——标准 BepInEx 装载路径上同名副本在类型扫描段即被身份折叠、到不了 Awake（实机实证：C4/C4' 两轮，含 BepInEx **惰性装载**机制：插件程序集在各自实例化拍才进 AppDomain）；改以 `Assembly.LoadFile`（绕过身份绑定的**非标准装载通道**）注入两个同名副本后，BUE Awake 扫描见 3 个同名实例，**逐条两条 001 Warning**（conflictLocation=%TEMP% 副本、selfPath=正版路径，字段与本节冻结票面逐字一致）+ 管理面板底边红色状态行，正版未被劫持（identity 行仍指 plugins+候选哈希）。**001 作为「非标准装载路径进来的副本的兜底网」由此从推论升格为实机证据**（威胁模型的忠实模拟：rogue loader 正是绕过身份绑定注入副本的通道）。
- **附注修订**：无冲突部署零 001 行在三环境（SP/P2P/U3DS）× 多轮全部成立；「冲突部署出现 Warning 诊断行与面板红色状态行」在**标准 BepInEx 插件包装的同名副本**上不成立（副本被引擎层折叠于上游），该期望**仅适用于绕过身份绑定的装载通道**（C4'' 已实证该通道下 001 逐条触发+面板红行）。
- **防双装三层图景（实机确立，双向验证）**：引擎身份折叠（标准路径上游阻断，C4/C4' 实证）→ BUE-PLATFORM-001（AppDomain 内检测，红测+C4'' 实机正向双证）→ 建议性处置（移除非官方副本；BUE 不删除任何文件，实机核验探针原封）。



