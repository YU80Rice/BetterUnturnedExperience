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

**登记工件（`FeatureDefinitionArtifact`）**：第 4 步交给注册桥的那个注册对象里，`Definition` 属性就是登记工件——它是**生产登记必经**的 DTO（受理门禁逐项校验它，校验不过即拒），不是「本版本未实装」的治理类型（未实装清单见 A.8）。工件的六个字段与生成姿势：

- `Feature`=你的 FeatureId；`FormatVersion`=工件格式版本（当前 **1**）；`DefinitionSetId`=你自己的集合标识串（自由文本，受理不做内容校验）；`CanonicalPayload`=你的权威载荷字节（**不得为空**）；`ArtifactPayloadDigest`=载荷摘要。
- **摘要必须等于 `FeatureDefinitionDigest.ComputeArtifactPayloadDigest(CanonicalPayload)` 的输出**——这是契约上的公开生成函数（SHA-256 摘要按 little-endian 切成 `Digest256` 四段），**不必也不许手抄常量**：受理门禁用同一个函数重算并与你给的摘要逐段比对，不符即拒 `InvalidDefinitionArtifact` / `BUE-REG-004`。
- `DefinitionSetDigest` 是自由格式的身份标记（本版本按哨兵值登记，受理不校验其算法）；载荷真实性由 `ArtifactPayloadDigest` 承担。
- 活样板 `NoOpFeaturePlugin.cs` 即按此姿势构造工件（载荷字节 + 公开函数输出 + 空/非空校验），照抄骨架时把载荷换成你自己的。

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

**当前契约版本：2.1**（宿主注册门槛 `SupportedContractMajor = 2`、`SupportedContractMinor = 1`——机器事实，自 DEV-V3-01 起成立）。**对外发布态已翻转为 2.1**：2026-09-11 DEV-V3-09 唯一候选经 SP/P2P/U3DS 三环境实机验收全判据通过 + 人工发布批准，`audit/RELEASES.md` 当前发布物 = 行 11（候选 SHA-256 `ce0d2191…17e413`）+ `publish/第3阶段-正式交付版本/` 交付包；2.0 基线 v8 已退役归档（§C.1/C.5）。

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

### 2.1（2026-09-11，DEV-V3-01..08 单一批次，加性变更不升 Major）

第三阶段八个平台缝的加性契约以**单一 Minor 批次**合入 2.1：宿主注册门槛 `SupportedContractMinor` 开至 1，**破坏性变更为零**——既有 2.0 模块**零重编译继续注册、继续运行**（`ContractIncompatible`/`BUE-REG-006` 仅当 `Major ≠ 2` 或 `Minor > 1` 时拒）。逐票加性条目、服务参考、码表与迁移指引**不在本节重复**，统一见**附录 C.1（总账）、附录 A（平台服务参考七节）、附录 B（诊断与身份码表）、附录 C.2（安全降级原则）**。本节只登记版本事实与批次构成：

- **加性面构成（按票）**：01 官方身份白名单拒绝 `ReservedFeatureId`/`BUE-REG-010` + Bootstrap 五成员永非 null 承诺；02 事件类型归属登记 `IFeatureEventRegistry`（`bootstrap.EventRegistry`）+ 路由归属不变量；03 生命周期状态投影 `IFeatureLifetime.CurrentStatus` + `TryTrack` 接线（容量 64/代际）；04 网络预算 `NetworkSendResult.Throttled=205` + 主线程投递 `IFeatureMainThread`（`bootstrap.MainThread`）；05 宿主时钟八条语义登记（**零新增契约面**）；06 设置 facet `IFeatureSettingsRegistration`（类型发现式可选面，`IFeatureRegistration` 成员一个未加）+ 双 scope 权威门；07 诊断 `IFeatureLogger` 接线 + 有界摘要 + `BUE-*` 前缀纪律（**零新增契约面**，兑现矩阵行）；08 SDK 附录 A/B/C 总装 + NoOp 统一生态契约 probe + 上架前自检清单。
- **加性理由（§C.3 登记纪律）**：本批次破坏性变更为零，全部落在 §C.3 加性四类之内——**新枚举值**（`ReservedFeatureId=206`、`Throttled=205`、各 `BUE-*` 观察/拒绝码）；**新矩阵成员**（`IFeatureBootstrap`/`IFeatureLifetime` 由宿主实现、模块只读，其成员自 01 阶段基线即以 null 预列于可用性矩阵，后续票只「接线」把 null 兑现为可用——读取方向 + 矩阵 null 容忍纪律，2.0 模块零破坏）；**新可选面**（设置 facet `IFeatureSettingsRegistration` 经**类型发现**，不加载既有 interface）；**新构造器**（`FeatureStatusView`/`NegotiatedFeatureView` 可构造）；05/07 为**零新增契约面**（仅语义登记 + 既有形状兑现）。**关键非破坏约束在实现者侧**：第三方 DLL 自己实现的 `IFeatureRegistration` 成员**一个未加**（恰 4 属性不变——加载成员会打断全部 2.0 外部实现者，06 论证）；宿主侧接口加成员不受此约束（模块只读不实现）。按 §C.3 规则维持 Major=2、仅升 Minor。
- **发布绑定**：对外 2.1 = DEV-V3-09 唯一候选 DLL，经单机/P2P/U3DS 三环境实机验收 + assembly-identity SHA-256/CaseId + 人工批准 + `audit/RELEASES.md` 行 + publish 交付包同步换新（§C.5 实施发布链顺序）；01..08 的中间构建不授 2.1 对外身份（候选纪律）。

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




---

# 附录（DEV-V3-08：平台服务参考 · 诊断与身份码表 · 契约版本与迁移）

> **定位与纪律**：正文八节冻结不动（2.0 基线原文保留——冻结的是 §7 的 ①–⑦ 基线条目原文，而「当前契约版本」活账与 §7 各版本小节随发布票追加/更新，见 C.1 翻转执行记录）；第三阶段（DEV-V3-01..07，契约 Minor 批次 **2.1**）的全部开发者承诺在此成文。附录条目=对应实施票已裁决内容的落档（每节标注出处票），**不新增任何运行时成员、不改变任何既有 interface**——文档不独立创造契约（V3-T9 裁决⑥）。
>
> **统一活样板**：本附录所有生态路径示例逐字取自 `src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`——该样板已被 Plugin.Tests 的「DEV-V3-08 统一探针」组在真实宿主组合（公开注册桥+真实 StartCatalog+生产总线/时钟/设置/诊断）下全链验证。文档示例与样板**双向锚定**由机器执行：示例代码行=样板源码逐字、样板身份串=文档引用一致（红测锚「SDK 附录与活样板双向锚定」子组）。编译期验证工具不在本阶段建设（T9 裁决①），「示例可编译」的落实形态=示例即被测试在跑的活样板本体。
>
> **统一生态契约 probe（分 seam 判据）**：NoOpFixture 是全链接入的生态侧总探针，链序冻结为 **注册→Bootstrap→Events→TryTrack/Lifecycle→Network→HostTick→Settings→Logger→停止与隔离**。每枚缝步有独立判据（`ProbeStepOutcome` NotRun/Passed/Mismatch + 各自的显式结果与结构化诊断行）：一步 Mismatch **不遮蔽**其余步，未跑到的步=**NotRun≠Passed**；注册被拒=链不运行（`LastProbe=null` 与「跑过」显式可区分）。失败分 seam 可定位由「一次红一缝」旋钮红测证明：旋钮只错置该步的期望，真实拒绝来自真实缝（总线归属路由/通道门禁/乐观并发/资源账/标识符消毒）。运行该探针组：`BetterUnturnedExperience.Plugin.Tests.exe --bue-v3-probe-red`。

## 附录 A：平台服务参考

### A.1 Admission 与 Bootstrap（出处：DEV-V3-01 / V3-T2）

**公开面清单（冻结）**：注册桥 `BueRuntimeHost.Register(IFeatureRegistration)` → `FeatureRegistrationResult`（Accepted/FeatureId/Reason/DiagnosticId 四元组）；`FeatureRegistrationPhase`（HostStarting/RegistrationOpen/CatalogFrozen/RuntimeReady/CoreSafeMode）；`FeatureRegistrationReason`（冻结枚举，含 2.1 加性值 `ReservedFeatureId=206`）；`IFeatureBootstrap` 恰 **12** 成员（DEV-V6-05 加性：`Patching` 补丁口袋，C.1 该批次条目）。拒绝=**显式结果，不抛越界异常**；受理携带 FeatureId 绑定与诊断标识。宿主尚未就绪的注册以 `HostUnavailable` 回报（跨桥路径诊断码 `BUE-HOST-001`，见 B.2）。

**判定顺序（冻结）**：阶段门（001/002/003）→ 基础/格式校验（004：definition null / FeatureId 空 / FormatVersion=0 / DefinitionSetId 空 / canonical payload 空或摘要不符；005：工厂 null）→ 保留段（010）→ 合同版本（006：Major 恰=2 且 Minor≤宿主支持批次）→ ClientUi 卫星（008）→ 设置 facet（011）→ 登记期属性读取异常折叠（009，拒因归 InvalidDefinitionArtifact）→ 锁内二次阶段校验+重复（007）→ 受理（ACCEPT）。逐码语义与作者处置建议见附录 B.1。

**官方身份白名单**：保留段=根串 `io.github.yu80rice.bue` 或其 `.` 前缀段；段内 FeatureId 须**恰等于**白名单枚举之一（BII、LIT、LIR、LHT、network、network.v1compat、noop 样例七项，逐值见 B.10），否则确定性拒 `ReservedFeatureId`/`BUE-REG-010`。白名单只裁决身份资格，**不授予任何契约面以外的特权**；不反射 caller、不读路径——身份=声明的 FeatureId 本身（部署事实由程序集身份与加载链承担）。

**Bootstrap 成员可用性矩阵**（spec 唯一口径；「阶段基线」=DEV-V3-01 组装承诺，「票后终态」=接线票完成后的组装事实；当前 01..07 已全闭环）：

| 成员 | 阶段基线（01 后） | 票后终态（当前） | 终态规则 |
|---|---|---|---|
| Identity | 可用 | 可用 | 冻结永非 null（绑定自身身份） |
| LifecycleGeneration | 可用 | 可用 | 冻结永非 null（宿主实发代际） |
| Events | 可用 | 可用 | 冻结永非 null |
| OwnedEvents | 可用 | 可用 | 冻结永非 null |
| Network | 可用 | 可用 | 冻结永非 null |
| EventRegistry | null | 02 后可用 | 宿主组装期永非 null |
| Lifetime | null | 03 后可用 | 同上 |
| Dependencies | null | 03 后可用 | 同上 |
| MainThread | null | 04 后可用 | 同上 |
| Patching | —（05 票加性成员） | 05 后可用 | 同上（宿主组装期永非 null；补丁口语义与码表见 C.1 该批次条目 / B.11） |
| Settings | null | 06 后可用 | **facet 规则**：注册声明设置 facet=组装非 null；未声明=诚实 null（不伪造设置） |
| Logger | null | 07 后可用 | 每功能一律非 null（无 facet 门） |

**FeatureScopeIdentity 字段可用性（01 票具名延期兑现，如实记载）**：`Id`/`DefinitionSetId`/`DefinitionSetDigest`=登记工件实发值（受理时绑定）；`FeatureVersion`/`CurrentSlug`=**公开注册桥路径下恒 null**——注册接口无此数据源，身份承诺只覆盖前三字段，作者不得依赖后两者；未来定义工件层引入数据源=契约登记事件再改本节。

**阶段基线容忍纪律**：面向未发布契约版本编码的模块仍须对「未接线成员=null」做容忍（矩阵行=按宿主契约版本的事实，不是编译期保证）；对 2.1 宿主，上表终态列即承诺。

**线程语义**：注册发生在插件发现/Awake 期；BUE GUID 硬依赖保证 BUE 先行——生态 Awake 时注册窗口已开。目录冻结后注册=PhaseClosed（显式拒，可观察）。

**内部登记记录**：受理即建 owner-scoped registration record（FeatureId/注册来源/状态/代际/资源所有权/停止与隔离结果）——宿主唯一事实载体，不退化为全局查找+散装静态表；**不公开 registration session**（公开面保持窄结果面，T2 裁决）。

**同权检验**：官方七身份与生态走同一桥同一规则（白名单正例锚=DEV-V3-01 常跑组）；NoOp 样例身份=白名单第七项，全链在 Plugin.Tests 真实组合下被验（本附录统一探针）。

**不承诺**：白名单增删属仓库治理（官方新模块随注册面演进）；`SupportedContractMinor` 数值随批次；不存在官方私有通道，也不存在生态专用旁路。

活样板登记姿势（`NoOpFeaturePlugin.cs`，逐字）：

```csharp
[BepInDependency("io.github.yu80rice.betterunturnedexperience", BepInDependency.DependencyFlags.HardDependency)]
// ...
public static FeatureRegistrationResult Register()
{
    return BueRuntimeHost.Register(ProbeRegistration);
}
```

### A.2 功能事件 FeatureEventBus（出处：DEV-V3-02 / V3-T3）

**路由不变量（冻结）**：内部路由索引=（EventId, EventType, 载荷类型归属 owner）三元组；**发布者 owner == 载荷类型归属 owner**；**一个载荷类型恰对应一个归属 EventId**（不存在「同类型不同事件互收」）；eventId 前缀校验保留（`<owner>/<event-name>`）。拒绝=显式结果+结构化诊断+**零派发**（任何订阅者都不被调用）。

**公开面（2.1 加性，形状冻结）**：`IFeatureEventRegistry.Register<TEvent>(string eventId)` → `FeatureEventRegistrationResult`（Registered/Reason/DiagnosticId）；`FeatureEventRegistrationReason : byte { None=0, InvalidEventId=1, EventIdNotDerivedFromOwner=2, EventTypeAlreadyRegistered=3, EventIdAlreadyRegistered=4 }`（值冻结，Contracts.Tests 锚定）。视图经 `IFeatureBootstrap.EventRegistry` 注入、绑定自身身份——不能替别人登记；宿主保留身份不可 mint 成登记视图（`EventRegistry(宿主标识)` 参数异常 fail-fast，与发布者视图同门）。既有两接口（`IFeatureEventSubscriber.Subscribe<TEvent>` / `IOwnedFeatureEventPublisher.TryPublish<TEvent>`）形状与语义**不变**；泛型 `Subscribe<TEvent>` 是便利入口，内部按登记的（EventId, Type）路由。

**顺序约束（冻结）**：生态自定义事件**必须**先经自己的 EventRegistry 登记类型归属，然后才能发布/订阅；登记时机=模块注册之后、首次发布/订阅之前。未登记类型：发布=显式拒绝+诊断；订阅=同样拒绝且按**开发期错误** fail-fast（同 null-handler 纪律；诊断行先于异常浮出）。官方事件类型（TidyCompleted→`io.github.yu80rice.bue.inventory-tidy`、HostTick→`io.github.yu80rice.bue.host`）由宿主在组合期唯一登记——不可重登记、不可改挂。

**重复与跨代幂等**：重复登记同一类型=**显式拒绝不覆盖**（BUE-EVT-003）；功能停止/隔离**不撤销**类型归属登记——再启用新代际重登记得到同样的显式拒，路由仍归首登 owner，其功能照常发布收帧（统一探针跨代路径即验证此姿势：把 `Registered || EventTypeAlreadyRegistered` 都视为路由在手）。

**线程与故障语义**：总线=进程内本地（跨机协作走 BueNetworkApi，不在此面）；派发在锁外快照执行；单 handler 异常隔离进诊断、不扩散到其它订阅者；订阅句柄独立幂等释放；功能停止边界由宿主统一 `UnsubscribeAll`——**模块无需也不应自拆订阅**。

**同权检验**：官方 LIT 经登记路径发布 TidyCompleted 的真实消费锚（DEV-V3-02）；宿主保留身份发布 HostTick；生态侧=统一探针 Events 缝（登记→订阅→自有 EventId 发布被收并自回帧→错挂声明被拒零派发）。

**不承诺**：不引入统一 envelope；`FeatureEventBus` 类本体保持内部自由（非契约面）；不提供持久事件队列/重放；不承诺多订阅者间的回调先后顺序。

活样板 Events 缝姿势（`NoOpFeaturePlugin.cs`，逐字）：

```csharp
public const string ProbeEventId = "io.github.yu80rice.bue.noop/probe-completed";
// ...
var registration = bootstrap.EventRegistry.Register<NoOpProbeEvent>(ProbeEventId);
probe.EventsRouteOwned = registration.Registered
    || registration.Reason == FeatureEventRegistrationReason.EventTypeAlreadyRegistered;
var subscription = bootstrap.Events.Subscribe<NoOpProbeEvent>(e => probe.EventsSelfReceived++);
probe.EventsPublishAccepted = bootstrap.OwnedEvents.TryPublish(publishId, new NoOpProbeEvent(1UL));
```

### A.3 生命周期与资源（出处：DEV-V3-03 / V3-T4）

**唯一状态投影（冻结）**：`FeatureState`/`FeatureStatusView`/`StateRevision` 是功能状态的**唯一**事实投影——散装布尔标志收编为宿主内部实现；**模块不能修改自己的状态**，一切状态变更由宿主驱动、经状态投影与 `FeatureStatusChangedEvent` 呈现（**事件呈现本版本未接线**：发布侧没有生产路径，只看 `CurrentStatus`/`StateRevision`——见 A.8）。`FeatureStatusView` 为不可变快照（六成员：Feature/State/Error/StopReason/DiagnosticId/StateRevision），面板与生态共用同一投影。

**只读状态查询最小行为面（冻结五条）**：`IFeatureLifetime.CurrentStatus` 接线后——任何阶段查询可用、不抛异常、不返回 null；返回不可变投影快照（不暴露内部可变引用）；隔离/停止后仍可用并**如实**返回当时状态；查询范围仅限自身 FeatureId（视图组合期已绑定身份）；模块无 mutator 面。

**TryTrack 资源账（冻结）**：`bool IFeatureLifetime.TryTrack(IDisposable)` 登记功能拥有的资源；停止时按注册**逆序**自动 Dispose；单资源 Dispose 异常隔离进诊断（BUE-LIFE-006）并继续清理其余；容量上限 **64/功能/代际**（V3-T4 冻结数值，超限显式拒 BUE-LIFE-002）；已停止/隔离功能不可再登记（拒绝序与逐码见 B.4）。统一探针以两资源对（first→second 登记、second→first 释放）把逆序事实做成可观察记录。

**代际与隔离**：两代际轴分离——LifecycleGeneration（功能启停代际）与 ConnectionGeneration（网络会话代际）互不映射；再启用=**新代际，旧代际一切失效**（旧视图/旧句柄/旧跟踪资源账全部作废并留显式诊断）；**Isolated 不自动重启**——只有用户经面板（command seam）显式启用才重新臂起，且从干净代际开始。`CoreSafeMode` 只由组合期不变量损坏触发（目录冻结失败/核心 capability 组合失败等）；运行期单功能失败**永不升级**，只走功能级隔离。

**Dependencies（冻结语义）**：只读目录能力查询（`Has`/`TryGet`），**不是求解器**、不做拓扑排序、不代拉依赖；presence-only 投影规则=空能力串+0 版只答「在不在」，非空能力声明 fail-closed（见 03 票）。**能问、本版本没有可问的能力**——能力协商协议未启用，逐条见 A.8。

**顺序约束**：`IFeatureModule.Start(IFeatureBootstrap)` 返回显式 `FeatureStartResult`；返回 false 或抛异常=该功能隔离（不炸宿主、不波及其他）；`Stop(reason)` 由宿主在停止边界调用，宿主随后完成资源逆序释放与订阅注销——模块的 Stop 内发布仍会被派发（停止故障不跳过清理）。

**同权检验**：面板启停 seam（`SetFeatureEnabled`）官方与生态同一条命令路径；官方 LIT 走真 TryTrack（网络句柄）+真 UserDisabled 循环（DEV-V3-03 官方锚）；生态侧=统一探针 Lifecycle 缝+停止/隔离边界子组。

活样板 Lifecycle 缝姿势（`NoOpFeaturePlugin.cs`，逐字）：

```csharp
first = new ProbeResource(probe, "first");
probe.Tracked = bootstrap.Lifetime.TryTrack(first);
second = new ProbeResource(probe, "second");
probe.TrackedSecond = bootstrap.Lifetime.TryTrack(second);
probe.QueriedStateAtStart = bootstrap.Lifetime.CurrentStatus.State;
```

**不承诺**：registration session 不公开（内部记录非契约面）；面板按钮 UI（Unity 控件）与宿主内部 command seam 是两回事（后者非 SDK 面）；StateRevision 只承诺单调，不承诺连续。

### A.4 网络与主线程投递（出处：DEV-V3-04 / V3-T5）

**发送预算（本票定值，冻结）**：平台按会话保底限流——每会话（ConnectionGeneration）固定窗 **2000ms 内 256 条**作者数据发送；超窗即重置；预算随代际隔离（跨代清零）；官方与生态一视同仁；**不静默丢弃**——超限返回新枚举值 `NetworkSendResult.Throttled = 205`（2.1 加性），控制帧/握手帧属平台内部流量不过预算。旧模块安全降级纪律：**未知/新结果值不得当成功**（只有显式 `Sent` 是成功）。

**聚合结果语义（既有冻结+本票扩展）**：全送达→`Sent`；全传输失败→`LocalTransportUnavailable`；混合（含被节流未执行）→`PartialFailure`；纯节流（无执行无失败）→`Throttled`；通道未注册仍优先 `ChannelNotRegistered`；空快照→`NoSession`。发送不持状态锁。

**主线程 dispatcher（2.1 加性面）**：`IFeatureBootstrap.MainThread`（`IFeatureMainThread`）**恰一个投递方法** `MainThreadPostResult Post(Action task)`，单向 fire-and-forget（任务无返回、无等待句柄；需要结果走事件回发或网络响应）。最小行为面冻结：投递返回显式结果（成功/容量拒绝 `CapacityExceeded`/已失效 `GenerationInvalid` 可区分），调用本身不抛越界异常（null task=开发期错误 fail-fast，先浮出 BUE-MT-004 行再抛）；模块停止/隔离/宿主停止后投递=显式失败+诊断；超限=显式失败+诊断不静默丢；任务绑定提交时模块的 LifecycleGeneration，代际失效后未执行任务不再执行；执行期单任务异常隔离进诊断（不扩散、不打穿主线程泵）。数值（本票定）：全局队列容量 **256** 待处理任务、每拍至多执行 **32**。**禁自建泵**——平台唯一主线程泵链，宿主停止后重开代际可再臂（同进程 reload 语义，边界≠死刑）。

**入站线程语义（SDK 冻结登记）**：入站网络回调运行在**传输泵线程**，不是 Unity 主线程——其中不得触碰 Unity 对象；需要主线程处理时经 `MainThread.Post` 转抛（这是平台认可的唯一线程交接）。不做「入站回调改主线程」（阶段决定）。

**会话链路健康（电平式诊断）**：连续传输失败达阈值 **10**（DEV-V2-25 先例冻结）→**一次** degraded 诊断（BUE-NET-002）；此后成功→**一次** recovered+计数清零（BUE-NET-003）；每会话代际独立。`Throttled`/参数门拒绝**不计入失败**（未执行≠传输失败）；`PartialFailure` 的失败目标计该会话失败、送达目标计恢复。作者据此定位「发送持续失败」是链路问题而非自身协议问题。官方 LIT 的告警限频被链路健康接管后退役（04 票）；业务重试与退避**不上收**（LIT 挑战重臂保留功能私有）。

**回放失败投影（冻结实现要求）**：`DeferredBueNetworkApi` 的 Attach/replay/detach 失败必须进统一诊断 sink（BUE-NET-005），并可区分四态=not-ready / detached / replay-failed / transport-unavailable；实现**不得以空 catch 无痕折叠**；每 episode 至多一条（防逐帧刷屏）。

**错误模式总表**：预算拒绝=BUE-NET-001 行+`Throttled` 结果；入站 handler 异常=BUE-NET-004 结构化诊断（不扩散不打穿泵）；投递三态=显式 `MainThreadPostResult`+BUE-MT-* 行。逐码见 B.5。

**同权检验**：LIR 迁移为 dispatcher 官方先行消费者（入站泵线程帧→执行只发生在 dispatcher 泵拍，DEV-V3-04 锚）；预算对官方生态一视同仁；生态侧=统一探针 Network 缝五判据（通道登记/established 空快照/`NoSession` 降级/负通道 `ChannelNotRegistered`/投递受理——宿主无会话环境下的**显式结果**即降级契约的活演示）。

**不承诺**：不新增网络状态查询 API（链路健康以诊断呈现）；不做可靠通道分档/加密/跨服中继（Out of Scope）；预算数值属平台治理（调整=契约登记事件）；`Sessions` 只含 established（2.0 ④ 冻结）。

活样板 Network 缝姿势（`NoOpFeaturePlugin.cs`，逐字）：

```csharp
var channel = new FeatureId(ProbeChannelId);
var registration = bootstrap.Network.RegisterChannel(channel, new ContractVersion(2, 0), 1);
probe.NetworkSessionsAtStart = bootstrap.Network.Sessions == null ? -1 : bootstrap.Network.Sessions.Count;
probe.NetworkSendObserved = bootstrap.Network.SendToClients(sendChannel, new byte[] { 1 }, false);
probe.MainThreadPosted = bootstrap.MainThread.Post(() => { }).Posted;
```

### A.5 宿主时钟 HostTick（出处：DEV-V3-05 / V3-T6；零新增契约面）

**八条语义（登记为契约，锚=DEV-V3-05 红测组）**：

1. **宿主产生与身份（防伪造）**：Tick 只能由宿主经保留身份 `io.github.yu80rice.bue.host` 产生；`Publisher(宿主标识)`/`EventRegistry(宿主标识)` fail-fast——任何功能都无法伪造宿主时钟。
2. **每拍恰一 tick（去重归宿主）**：每泵拍至多一个；同帧多驱动源的去重是宿主实现责任，订阅者无需自行去重。
3. **Phase 冻结**：`Phase=Update`、数值=0；新增阶段=契约登记显式加性扩展，不得隐式增加（Contracts.Tests 锚=枚举恰一成员，隐式扩展必红）。
4. **序号**：`TickNumber` 从 1 起严格单调 +1、时钟生命周期内不重置；功能不可修改；失败拍不消耗序号；模块代际变化不改变序号语义。
5. **DeltaTime**：相邻 tick 单调时差；首拍=0；时钟回拨/负差**钳零**且基线停留高水位；**暂停期间无 Tick**（无拍即无产生），恢复后下一拍 DeltaTime=实际间隔、**无追帧**——是否忽略大间隔由功能自决。
6. **载荷范围**：只含时序三字段（Sequence/DeltaTime/Phase）——不得携带业务字段、网络消息、设置值、玩家状态、功能命令、诊断载荷（Contracts.Tests 形状锚=恰三属性，隐式加字段必红）。
7. **异常语义**：派发不把订阅者异常抛回泵调用方；时钟自身故障=显式 false 返回+结构化诊断（`BUE-CLOCK-001` 宿主观察行）+零派发+失败拍不消耗序号/不移动时间基线。
8. **主线程构造性保证**：时钟无内部线程、无隐藏队列——handler 在 `Tick()` 调用方线程（宿主 Update 链=主线程）上同步执行；生态可在回调内安全调用主线程限定 API。

**自节流=官方推荐模式（登记）**：宿主时钟**无独立 Hz 承诺、无调度协商**——需要低频逻辑的功能订阅 HostTick 后在功能内部自节流（累计 DeltaTime 阈值或序号差值两式；官方先例=LHT 10Hz HUD：`UpdateIntervalSeconds=0.1f` 实现常量、节奏时钟=宿主累计秒）。配套纪律：首拍 DeltaTime=0 按「可能不干活」写码；回拨已钳零无需功能侧防御；模块停止时清理自身节流状态；**禁止自建第二个 Unity Update 泵**（平台唯一泵）。派生低频时钟、per-feature 调度参数=需求信号雾区（Out of Scope，不预建）。

**同权三条（T6 裁决六）**：官方与生态同一 HostTick 订阅 seam（LHT 真订阅锚+统一探针 HostTick 缝）；官方功能不得获得特殊 Tick 频率；生态功能不得被宿主静默过滤。

**版本规则**：本票零新增契约面、不触发版本变化——语义升格为登记契约（实现事实→文档承诺）。

### A.6 设置 Settings（出处：DEV-V3-06 / V3-T7）

**注入面（冻结形状）**：`IScopedFeatureSettings` **恰三方法**（`GetSnapshot(scope)` / `TryGet(id, out value, out revision)` / `Submit(ScopedSettingChangeRequest)`）——查询面不得扩（形状锚钉死：视图注入面永不含 ApplyServerPolicy/ClearSessionOverlay/ActivateConnectionGeneration 类运行时方法）。视图 `bootstrap.Settings` 绑定（功能, LifecycleGeneration）：**读永可用**（功能自身持久真相的只读观察，无突变=无跨代污染面）；**写经两道门**——代际失效/停止边界后显式拒（BUE-SET-001）；ServerAuthority scope 在非权威端显式拒（BUE-SET-002 + UnauthorizedSender）。

**facet 声明（2.1 加性可选面）**：注册对象可**额外实现** `IFeatureSettingsRegistration`（宿主经类型发现，**不**加在 `IFeatureRegistration` 上——2.0 外部实现者零破坏）：恰两成员 `SettingDescriptors`（功能拥有 Schema，宿主据此构造唯一 per-feature SettingsRuntime；descriptor 必须全携本功能 FeatureId、非空、≤**64/功能**，违规=注册拒 `InvalidDefinitionArtifact`/`BUE-REG-011`）与 `OnSettingsApplied`（面板编辑生效后的功能私有刷新钩子，可 null；刷新钩子在触发时刻对登记对象**活解析**，快照只存 resolver 不存可能过期的委托）。未声明 facet 的功能：`Settings` 诚实 null、面板**不伪造设置页**（回退编辑器拒编辑留痕 BUE-SET-004）。

**宿主规则（官方生态共用，同一 runtime 单源）**：校验/revision 单调/损坏安全默认/原子提交/作用域隔离；per-feature **恰一** SettingsRuntime（同 root 键控；schema 冲突 KeepExisting 显式拒 BUE-SET-003——防第二事实源）；再启用新代际**续用同一持久真相**（revision 不清零）。持久化文件布局不变（`<FeatureId>.<Scope>.bue-settings`，玩家既有设置无缝续用）；契约侧不硬编码路径。

**双 scope 语义（冻结）**：`ClientPreference`（本地偏好）与 `ServerAuthority`（服务器权威）；U3DS 与 P2P listen host 的主机权威端=**同一 provider 同一代码路径同语义**（红锚双侧钉）；客户端会话覆盖（SessionProjection）**断线清除**、永不污染持久化 revision；`ExpectedRevision` 防旧 UI 覆盖新值（乐观并发，过期=显式拒）；`schemaVersion` 通道保留、迁移由功能自理；**不做跨机同步协议**（服务器→客户端 policy 投递与断线清除的生产驱动=Network 域另立票）。

**生态五不得（裁决文）**：不另造配置格式、不绕过作用域、不直写持久化、不假设他人作用域可读、不做跨机同步。

**面板=编辑 adapter（非第二事实源）**：面板按注册目录动态路由（官方硬编码清单已退役）；面板快照与功能视图快照恒等（同 revision 同 entries）；编辑链=面板→目录路由→宿主唯一 runtime→`OnSettingsApplied`→功能经注入 view 读到新值（全链单真相，官方先行消费锚=真实 LIT/LIR/LHT/网络模块全迁移到注入 view）。

**同权检验**：统一探针 Settings 缝五判据（读快照/合法提交 revision 推进/未知 id 拒/过期 ExpectedRevision 拒/停止边界写拒）；跨重启续账判据在停止与再启用子组。

**不承诺**：`SettingsRuntime`/`ISettingsPersistence`/面板 editor 类本体不列契约（类本体自由）；平台统一迁移框架不做；跨机设置同步协议 Out of Scope。

活样板 Settings 缝姿势（`NoOpFeaturePlugin.cs`，逐字）：

```csharp
var snapshot = settings.GetSnapshot(SettingRevisionScope.ClientPreference);
var commit = settings.Submit(new ScopedSettingChangeRequest(
    1000UL + snapshot.Revision, SettingRevisionScope.ClientPreference, snapshot.Revision,
    new[] { new SettingMutation("noop.probe-toggle", SettingValue.Toggle(!current.Boolean)) }));
probe.SettingsRevisionAdvanced = commit.Accepted && commit.Revision > snapshot.Revision;
```

### A.7 诊断与 Logger（出处：DEV-V3-07 / V3-T8）

**注入面（形状既有，07 兑现矩阵行）**：`IFeatureLogger` 三方法窄面（`Info(eventName, diagnosticId)` / `Warning(eventName, error, diagnosticId)` / `Error(eventName, error, diagnosticId, exception)`），每模块绑定自身 FeatureId 的 view，**每功能一律接线**（无 facet 门）；`Error` 的 fault=「类型:消息」**无堆栈**（不保存敏感 payload）。

**行为面八条（07 票登记正文）**：三方法 void=**无报错面**，隔离义务全在 view 侧——sink 恒抛与主+fallback 双故障均不外抛（故障观察=BUE-LOG-005 恰一条/episode latch）；每行盖自身 FeatureId+代际；代际撤账后（停用/隔离/启动失败/启用失败/宿主停止）写入=不产模块行+BUE-LOG-004 显式留痕一条（reason 区分 write-boundary/host-stopped）+静默计数；`OpenGeneration` 重臂=宿主停止是边界非死刑（reload 后可恢复）；前缀纪律按保留段身份判定（段内白名单身份经登记桥准入才存在——治理哲学=T2 同一条）；null/空白标识符=拒写+BUE-LOG-002；消毒=空白→下划线+截断（event **64** / diagnosticId **128** / fault **256**，本票定值），条目键=截断后值、伪造换行不产第二行。

**结构化行与统一 sink**：行写 BepInEx `LogOutput.log`（Info/Warning/Error→正常播放可见级别，非 Debug 静默通道；中文人读行走原通道不受改）；T4 隔离/T5 链路健康/状态投影行经统一摘要聚合（带码行按字段边界 token 聚合；无码行不聚合不造静默条目）——**分 seam 判据可定位、互不遮蔽**；设置拒绝/事件回调/dispatcher 拒绝行保留各自既有缝（收编面以 spec 三缝为准）。

**有界诊断摘要（本票定值）**：按 **(FeatureId, DiagnosticId)** 聚合（级别=max 所见/计数/首末 UTC 时间）；容量 **128**——溢出=模块原行照写（证据链不被掐）+BUE-LOG-003 观察恰一条+新码不再聚合（内存有界）；输出限频=首见一条+每条目 **30000ms** 至多一条（只限摘要行，≠过滤原行）；纯内存=重启不持久；冻结行形：

```text
BUE diagnostic-summary featureId= diagnosticId= [level=] count= firstSeen= lastSeen=
```

**BUE-* 前缀纪律**：`BUE-*` 平台诊断前缀**保留**——保留段外的身份冒用 BUE-* 码=拒写+BUE-LOG-001（latch 一条+计数）；生态诊断码用 **FeatureId 派生前缀**（合法示例见 B.9）；段内白名单身份（如官方功能 BUE-LIT-*、样例 BUE-NOOP-*）经桥准入后放行——这正是官方先行行能通过的前置。

**导出与验收边界（冻结）**：BUE **不建**日志复制器/采集器/面板导出动作；原始日志导出归 UMM 人工流程（Player.log + LogOutput.log）；**摘要≠验收授权**——CaseId/RELEASES 仍走人工批准链（发布纪律 §C.5）。

活样板 Logger 缝姿势（`NoOpFeaturePlugin.cs`，逐字）：

```csharp
probe.LoggerAvailable = true;
logger.Info("noop-probe-info", "BUE-NOOP-INFO");
probe.LoggerInfoWritten = true;
logger.Warning("noop-probe-warning", FrameworkErrorCode.SettingRejected, "BUE-NOOP-WARN");
logger.Error("noop-probe-error", FrameworkErrorCode.None, "BUE-NOOP-ERROR", null);
```

**同权检验**：官方 LIT 启停两结构化行经注入 view（BUE-LIT-START/STOP，官方先行锚）；生态侧=统一探针 Logger 缝（三方法窄面+行级判据+摘要计数）。

**不承诺**：诊断实时视图、诊断附件 API、UMM 诊断包自动化=Out of Scope（spec 已钉）；摘要条目集合不是稳定查询 API（内存有界、重启即空、限频输出）。

### A.8 未实装（本版本）（出处：V6-T1/T4/T6；本文件唯一的未实装清单）

「未实装」= 契约面或词汇表已经承诺、但当前版本**没有任何生产路径经过**的机制（词条定义在 `CONTEXT.md`）。本文件只在本节列**契约面**的未实装项；词汇表层面的治理意图在 `CONTEXT.md` 词条内标注；开发手册只指向本节、不复述。判据纪律：不允许以「代码存在、测试覆盖、或写在别处」冒充已兑现。

- **能力协商协议（Hello / Snapshot / Ack）未启用**：能力询问窗口 `IDependencyCapabilityView` **已经宿主注入**（Bootstrap 可用性矩阵行）——作者**能问**，但**本版本没有可问的能力**：目录是 presence-only 投影（空能力串 + 0 版只答「在不在」），握手 / 快照 / 确认三段协议没有生产路径，官方功能零次 `Dependencies.Has`。
- **`FeatureStatusChangedEvent` 未接线**：A.3 承诺「状态变更由宿主驱动、经状态投影与 `FeatureStatusChangedEvent` 呈现」。本版本状态投影已接线（`IFeatureLifetime.CurrentStatus` 只读查询 + `StateRevision` 单调），但**事件呈现未接线**——宿主不发布该事件，不要据此写订阅侧逻辑。

**过界即违规**：把本文件别处已接线的东西（面板/设置/诊断/网络/主线程投递/补丁口袋）写进本节、或把生产必经的登记工件（§4「登记工件」）当未实装——两者都不是「未实装」，写进来就是把已兑现的承诺说成没兑现。词汇表层面的治理意图同样不进本节：它们不是契约面，那类词条的未实装标注在 `CONTEXT.md`（词条定义见其「未实装」条），本节只收契约面。

## 附录 B：诊断与身份码表

### B.0 总则（行形、级别与两分类口径）

结构化诊断行统一写入 BepInEx `LogOutput.log`（键值对形如 `event=<名> feature=<id> ... diagnosticId=<码>`；`BUE diagnostic-summary featureId=... diagnosticId=... level=... count=... firstSeen=... lastSeen=...` 为冻结摘要行形（A.7））。诊断码两分类（04/05/06/07 票统一口径）：

- **拒绝码表**（作者须按语义分支处置的显式拒绝）：B 节各表中列「拒绝」属性的码——`BUE-REG-*`、`BUE-EVT-001..004` 属之。
- **宿主观察行**（记录事实、不要求作者分支处置）：`*-ACCEPT`、`*-CREATED`、`*-GEN`、`BUE-LIFE-STATE/RELEASE/ISOLATE`、`BUE-CLOCK-001`、`BUE-PLATFORM-*` 等——它们**不入拒绝码表语义**，出现时按行内容理解，不触发作者侧降级分支。运行期内部拒绝行（LIFE/NET/MT/SET/LOG 家族的 `001..` 码）居中：由宿主在拒绝发生处写行，作者经**显式方法返回值**（TryTrack=false/Post 结果/Submit 结果）分支，诊断行供定位。

### B.1 注册拒绝码表 BUE-REG（判定顺序见 A.1）

| 码 | Reason（冻结枚举值） | 触发 | 作者处置建议 |
|---|---|---|---|
| `BUE-REG-ACCEPT` | None | 受理（观察） | 无需处置；FeatureId 绑定生效 |
| `BUE-REG-001` | HostUnavailable(100) | 宿主仍在 HostStarting 期 | 推迟注册（等 BUE 前置就绪；硬依赖下正常不会遇到） |
| `BUE-REG-002` | CoreUnavailable(900) | 宿主 CoreSafeMode | 禁用自身功能并提示用户修复 BUE 部署（等前置=不可用终态，不自愈） |
| `BUE-REG-003` | PhaseClosed(101) | 注册窗口已关（目录冻结/RuntimeReady 后） | 按「BUE 已越过装配点」降级：功能不启动，提示重载或重启游戏 |
| `BUE-REG-004` | InvalidDefinitionArtifact(201) | 定义工件无效（null/FeatureId 空/FormatVersion=0/DefinitionSetId 空/canonical payload 空或摘要不符） | 修工件（作者侧编码/打包错误，确定性失败） |
| `BUE-REG-005` | InvalidModuleFactory(204) | ModuleFactory null | 修注册对象 |
| `BUE-REG-006` | ContractIncompatible(202) | `MinimumBueContract` Major≠2 或 Minor 超批次 | 报版本不兼容（提示升级 BUE 或降低自身契约需求，§C.1/C.2） |
| `BUE-REG-007` | DuplicateFeature(200) | 同 FeatureId 重复登记 | 检查双份部署/重复调用（§6 双装场景联动 BUE-PLATFORM-001） |
| `BUE-REG-008` | InvalidClientUiRegistration(205) | ClientUi 卫星 id/token 空或契约不兼容 | 修卫星声明或放弃 UI 卫星降级 |
| `BUE-REG-009` | InvalidDefinitionArtifact(201) | 登记期读取注册属性抛异常（折叠保护） | 注册对象属性须无副作用不抛 |
| `BUE-REG-010` | ReservedFeatureId(206) | FeatureId 在官方保留段且不在白名单 | 改用作者反向域名自有 FeatureId（B.10 示例）——冒用官方身份**不会**成功 |
| `BUE-REG-011` | InvalidDefinitionArtifact(201) | 设置 facet 无效（空描述符/跨功能 id/超 64 条/FeatureId≠本功能） | 修 schema 声明（A.6 facet 规则） |

### B.2 平台自检码 BUE-PLATFORM

| 码 | 语义 | 处置 |
|---|---|---|
| `BUE-PLATFORM-001` | 双装冲突诊断（同程序集名不同来源副本已进入 AppDomain；正文 §6 冻结面） | 玩家按 conflictLocation 移除非官方副本；BUE 不删文件 |
| `BUE-PLATFORM-002` | 自检**自身**故障隔离行（扫描侧异常留痕、不阻塞启动；专码不挪用） | 无作者动作；反馈时随日志带走 |
| `BUE-HOST-001` | 注册时宿主尚未就绪（`HostUnavailable`）：公开注册桥在宿主组合完成前被调用即以此码返回（网络模块的登记路径同码） | 推迟到 BUE 前置就绪后再注册（硬依赖下正常不会遇到；引用面处置建议同 B.1 `BUE-REG-001`） |

### B.3 事件登记码 BUE-EVT（判定顺序=格式→前缀→类型→身份串）

| 码 | 语义 | 作者处置 |
|---|---|---|
| `BUE-EVT-ACCEPT` | 类型归属登记受理（观察行） | — |
| `BUE-EVT-001` | InvalidEventId（格式：缺 `/` 分隔、空 owner 段或空事件段） | 修事件身份串（`<自身FeatureId>/<event-name>`） |
| `BUE-EVT-002` | EventIdNotDerivedFromOwner（前缀≠自身 FeatureId——不能替别人登记） | 用自己身份派生 |
| `BUE-EVT-003` | EventTypeAlreadyRegistered（类型已挂他处/自家跨代重登；**不覆盖**） | 视为幂等：路由在手即可发布收帧（A.2 跨代幂等） |
| `BUE-EVT-004` | EventIdAlreadyRegistered（身份串已被占） | 换事件名 |

发布/订阅运行期拒绝走结构化 reason 行（`event=feature-event result=publish-rejected reason=type-not-registered|owner-mismatch|event-id-mismatch`、订阅未登记=`reason=type-not-registered`+fail-fast 异常），非 REG/EVT 拒绝码族——A.2 路由不变量的可观察面。

### B.4 生命周期码 BUE-LIFE

**观察行（不入拒绝语义）**：`BUE-LIFE-STATE`（每次合法状态迁移一行，含 to=/generation=/revision=/reason=）、`BUE-LIFE-ACCEPT`（TryTrack 受理）、`BUE-LIFE-RELEASE`（停止边界资源释放）、`BUE-LIFE-ISOLATE`（功能级隔离，含 stage=/error=）。

**TryTrack 拒绝码（拒绝序=null→状态门→代际→重复→容量）**：

| 码 | 触发 | 作者处置 |
|---|---|---|
| `BUE-LIFE-001` | 登记 null 资源 | 修代码（开发期错误） |
| `BUE-LIFE-003` | 状态门：非 Starting/Running（feature-isolated / feature-not-running / unknown-feature） | 停止/隔离后不再登记——资源只在活代际拥有 |
| `BUE-LIFE-004` | 视图代际过期（stale-generation） | 用**当前**注入视图登记（旧代际视图全失效，A.3） |
| `BUE-LIFE-005` | 同一实例重复登记（不双释放） | 去重自有资源 |
| `BUE-LIFE-006` | 停止清理期单资源 Dispose 抛异常（隔离+继续） | 资源自身 Dispose 须幂等无抛——作者侧 bug 信号 |
| `BUE-LIFE-002` | 容量超限（64/功能/代际，本票定值） | 收敛同代际资源数；平台不静默丢 |

### B.5 网络与投递码 BUE-NET / BUE-MT

**BUE-NET（运行期拒绝/健康观察）**：

| 码 | 语义 |
|---|---|
| `BUE-NET-001` | 发送预算拒绝（`Throttled` 伴随行，A.4） |
| `BUE-NET-002` | 会话链路 degraded（连续失败达阈值 10 恰一条） |
| `BUE-NET-003` | 会话链路 recovered（恢复恰一条+清零；002/003=电平式一对） |
| `BUE-NET-004` | 入站 handler 异常隔离（不扩散不打穿泵线程） |
| `BUE-NET-005` | 回放失败投影（reason 四态=not-ready/detached/replay-failed/transport-unavailable，禁空 catch） |

**BUE-MT（主线程投递）**：

| 码 | 语义 | 作者侧信号 |
|---|---|---|
| `BUE-MT-ACCEPT` | 投递受理（观察行） | — |
| `BUE-MT-001` | 队列容量拒绝（全局 256 待处理） | 显式 `CapacityExceeded`——降频/合并任务 |
| `BUE-MT-002` | 代际或作用域失效拒（停止/隔离/换代/宿主停止） | 显式 `GenerationInvalid`——别向旧代际投 |
| `BUE-MT-003` | 执行期单任务异常隔离 | 任务自身 bug 信号（不扩散不打穿泵） |
| `BUE-MT-004` | 无效任务（null 投递，fail-fast 先行浮出行） | 开发期错误 |
| `BUE-MT-005` | 非主线程泵拒绝 | 泵线程违例信号（A.4 禁自建泵） |
| `BUE-MT-006` | 宿主泵组合带失败 | 平台侧故障行，反馈带走 |

**宿主观察行**：`BUE-MT-GEN`（代际开账/撤账）、`BUE-MT-CREATED`（组合根行）——非拒绝语义，不入拒绝码表（分类口径见 B.0）。

### B.6 时钟码 BUE-CLOCK

`BUE-CLOCK-001`：宿主时钟自身故障观察行（`event=host-tick result=failed errorType=... message=... diagnosticId=BUE-CLOCK-001`）——显式 false 返回+零派发+失败拍不消耗序号（A.5 第 7 条）；宿主观察行，不入拒绝码表。

### B.7 设置码 BUE-SET

| 码 | 语义 | 作者侧信号 |
|---|---|---|
| `BUE-SET-001` | 视图写入被拒：代际失效/停止边界后（generation-invalid） | `Submit` 显式拒——用当前代际视图（A.6） |
| `BUE-SET-002` | 视图写入被拒：ServerAuthority scope 非权威端（not-authority-side） | `Submit` 显式拒（UnauthorizedSender）——按角色降级为只读 |
| `BUE-SET-003` | 同功能 schema 冲突：注册表 KeepExisting 显式拒（防第二事实源） | 单宿主内同功能只应有一个 runtime（正常不可达；插件自构 runtime 违例信号） |
| `BUE-SET-004` | 面板回退编辑器拒编辑未提供设置的功能（不伪造设置页） | 面板侧观察行，非作者面 |
| `BUE-SET-005` | 无效 schema 抵达注册表（登记侧同门=BUE-REG-011；直接构造注册表=宿主内部带子，均显式行不落空） | facet 声明或宿主组装违例信号 |

**宿主观察行**：`BUE-SET-CREATED`（每功能 runtime 恰一条）、`BUE-SET-GEN`（代际边界：generation-opened / owner-invalidated / composition invalidate 失败）——非拒绝语义，不入拒绝码表（B.0 口径）。

### B.8 诊断码 BUE-LOG

| 码 | 语义 |
|---|---|
| `BUE-LOG-001` | 保留段外身份冒用 `BUE-*` 诊断码=拒写（latch 一条+计数，不逐行刷） |
| `BUE-LOG-002` | 无效标识符（null/空白 event 或 diagnosticId）=拒写留痕 |
| `BUE-LOG-003` | 摘要容量溢出观察（128 上限；原行照写=证据链不被掐，新码不聚合） |
| `BUE-LOG-004` | 代际撤账后写入拒（reason 区分 write-boundary / host-stopped；再启用恢复） |
| `BUE-LOG-005` | Logger 内部故障隔离 fallback 行（主+fallback 双故障均不外抛，模块永不被日志反噬） |

**宿主观察行**：`BUE-LOG-CREATED`（组合根行）、`BUE-LOG-GEN`（代际账行）——非拒绝语义，不入拒绝码表（B.0 口径）。

### B.9 前缀纪律与生态诊断码命名

- `BUE-*` = **平台保留前缀**（诊断域）：保留段（B.10 的 FeatureId 段）内且经登记桥准入的身份才可写 `BUE-*` 码（官方功能 BUE-LIT-*、样例 BUE-NOOP-* 属之）；段外身份冒用=拒写+BUE-LOG-001（治理哲学=T2/010 同一条：身份段决定码段资格）。
- 生态诊断码命名=**FeatureId 派生前缀**：建议形态 `<feature 末段>-<序号/语义名>`，例：`com.acme.medical-overlay` 功能用 `medical-overlay-001`；与平台保留段互不遮蔽、互不冒充。
- 标识符消毒（写前统一执行，A.7）：空白→下划线；截断=event 64 字符、diagnosticId 128 字符、fault 256 字符；伪造换行不产第二行；摘要条目键=截断后值。

### B.10 FeatureId 保留段与合法/非法示例

- 保留段根：`io.github.yu80rice.bue`（恰等或其 `.` 前缀段）。段内白名单七值（唯一可注册身份，01 票冻结枚举）：`io.github.yu80rice.bue.better-item-interaction`（BII）、`io.github.yu80rice.bue.inventory-tidy`（LIT）、`io.github.yu80rice.bue.in-place-reload`（LIR）、`io.github.yu80rice.bue.horde-tracker`（LHT）、`io.github.yu80rice.bue.network`、`io.github.yu80rice.bue.network.v1compat`（官方网络模块对）、`io.github.yu80rice.bue.noop`（样例生态身份）。段内另有非注册功能保留身份 `io.github.yu80rice.bue.host`（宿主时钟/总线保留，任何功能不可 mint）。
- **合法示例**（生态作者反向域名）：`com.acme.medical-overlay`、`io.gitlab.acme.bue-overlay-pack`、`dev.someone.custom-hotkeys`——段外身份，`BUE-*` 诊断码不可用作其 diagnosticId（B.9）。
- **非法示例**（确定性拒 BUE-REG-010/`ReservedFeatureId`）：`io.github.yu80rice.bue.medical-overlay`（冒用官方段）、`io.github.yu80rice.bue`（恰等根串）、`io.github.yu80rice.bue.host`（宿主保留）。
- 身份纪律（正文 §2 延伸）：FeatureId≠GUID≠文件名≠路径；BUE 插件 GUID `io.github.yu80rice.betterunturnedexperience` 与程序集名 `BetterUnturnedExperience` 冻结；不反射 caller、不读部署路径判定身份。

### B.11 补丁口袋码 BUE-PATCH（出处：DEV-V6-05 / V6-T5 Q2）

**观察行（不入拒绝语义，B.0 口径）**：`BUE-PATCH-ACCEPT`（句柄受理——句柄随即进**既有资源跟踪账**，与其它受跟踪资源同 (FeatureId, 代际)、同逆序释放；进出账沿用 B.4 的 `BUE-LIFE-ACCEPT`/`BUE-LIFE-RELEASE` 行）。

**登记拒绝码（作者按显式 `FeaturePatchRegistrationResult.Reason` 分支）**：

| 码 | Reason（冻结枚举值） | 触发 | 作者处置 |
|---|---|---|---|
| `BUE-PATCH-001` | InvalidPatch(1) | 传入 null 句柄 | 修代码（开发期错误） |
| `BUE-PATCH-002` | FeatureNotRunning(2) | 功能不在运行态（已停止/隔离） | 停止边界后不再登记补丁 |
| `BUE-PATCH-003` | GenerationInvalid(3) | 视图代际过期 | 用**当前**注入视图登记（旧代际视图全失效） |
| `BUE-PATCH-004` | DuplicatePatch(4) | 同一实例重复登记（不双拆除） | 去重自有补丁 |
| `BUE-PATCH-005` | CapacityExceeded(5) | 每代际容量超限（与其它受跟踪资源**同一** 64/功能/代际 上限） | 收敛同代际资源数；平台不静默丢 |

**拆除所有权（契约诚实，不宣称沙箱）**：经本口登记并受理的句柄由平台在停止/隔离边界拆除（句柄 `Dispose` = 拆除动作）；**未经此口挂的补丁，平台不保证拆除**。官方先行消费者=更好的物品交互（BII）：两处 Harmony 经此口登记，模块侧登记/拒收行 `BUE-BII-003`/`BUE-BII-004`（官方功能自有码，段内身份经桥准入——B.9 前缀纪律）。整理（LIT）随后也按同一接入面接入：全部整理补丁面共用一个 Harmony 身份、一枚句柄进账，模块侧登记/拒绝/缺口袋行 `BUE-LIT-PATCH-001`/`002`/`003`、平台释放行 `BUE-LIT-PATCH-004`、账外自拆留痕 `BUE-LIT-PATCH-005`（DEV-V6-11）。官方功能按「**武装 ⇒ 已登记**」建成：缺口袋或登记被拒即立即自拆，平台账外不留补丁——这是推荐给第三方功能的同一条纪律，不是额外特权。原位换弹（LIR）同律接入：全部换弹补丁面（权威两压弹面 + 弹药 HUD 面 + 技能分区面）共用一个 Harmony 身份，**一个生命周期代际恰登记一条**（登记行 `BUE-LIR-PATCH-001`；拒绝/缺口袋 `BUE-LIR-PATCH-002`/`003`；账边界释放行 `BUE-LIR-PATCH-004`；账外自拆留痕 `BUE-LIR-PATCH-005`；开关撤销遇拆除故障 `BUE-LIR-PATCH-006`；上一套补丁可能仍在架且反转未成功时**拒绝重复安装**（fail-closed，保持原版语义）`BUE-LIR-PATCH-007`，DEV-V6-12）。设置开关可在同一代际内反复拆装，形状是：**热摘 = 经该句柄跑同一条拆除动作**（不另起 `UnpatchSelf`）、**热装 = 重新武装同一身份**（不新增登记条目、不换句柄）——一句话：登记是代际级的，拆装是开关级的；账不随开关累积，每代际容量与开关次数无关，账上也不会留下已拆句柄。平台在停止/隔离边界释放那一条；届时若已被热摘，拆除动作按「当前无在架补丁」空操作返回（同一套补丁不会被拆两次）。尸潮播报（LHT）同律接入：信标权威面 + 可选 HUD 画面面共用一个 Harmony 身份、一枚句柄进账，**一个生命周期代际恰登记一条**（登记行 `BUE-LHT-PATCH-001`；拒绝/缺口袋 `BUE-LHT-PATCH-002`/`003`；账边界释放行 `BUE-LHT-PATCH-004`；账外自拆留痕 `BUE-LHT-PATCH-005`；开关撤销遇拆除故障 `BUE-LHT-PATCH-006`；上一套补丁可能仍在架且反转未成功时拒绝重复安装 `BUE-LHT-PATCH-007`，DEV-V6-13）。尸潮的设置开关是**全停开关**（off = 追踪退订 / 频道注销 / 补丁原生回退；on = 整链重臂），补丁那一段与换弹同一条纪律：热摘 = 经该句柄跑同一条拆除动作，热装 = 重新武装同一身份；**无画面只砍画面面**（HUD 补丁不武装、诊断如实），权威信标面仍武装且仍登记——即「缺画面不减所有权移交」，权威计数与广播照常。

## 附录 C：契约版本与迁移

### C.1 2.0→2.1 加性条目总账（DEV-V3-01..07 单一批次，Minor 2.1；若实际分批则顺延 2.2）

| 票 | 加性条目（全部向后兼容，2.0 模块继续可注册可运行） |
|---|---|
| 01（V3-T2） | `FeatureRegistrationReason.ReservedFeatureId=206`；白名单+`BUE-REG-010`；宿主 `SupportedContractMinor` 门槛开至 1 |
| 02（V3-T3） | `IFeatureEventRegistry` + `bootstrap.EventRegistry`（11 成员之落位）；`FeatureEventRegistrationReason/Result`；`BUE-EVT-*` 码族；路由归属不变量（A.2） |
| 03（V3-T4） | `IFeatureLifetime.CurrentStatus`（只读查询）；`FeatureStatusView` 六成员构造器、`NegotiatedFeatureView` 七参构造器（可构造结果先例=DEV-V2-21）；TryTrack 容量 64/代际；`BUE-LIFE-*` 码族 |
| 04（V3-T5） | `NetworkSendResult.Throttled=205`；`IFeatureBootstrap.MainThread` + `IFeatureMainThread`/`MainThreadPostResult`/`MainThreadPostReason`；预算 2000ms/256、队列 256/每拍 32、链路阈值 10；`BUE-NET-*`/`BUE-MT-*` 码族 |
| 05（V3-T6） | **零新增契约面**——HostTick 八条语义+自节流模式升格为登记契约（A.5）；`BUE-CLOCK-001` 观察行 |
| 06（V3-T7） | `IFeatureSettingsRegistration`（恰两成员，类型发现式可选面；`IFeatureRegistration` 恰 4 属性不变）；`FeatureRegistrationEntry` 目录投影加性两属性；`BUE-REG-011`；`BUE-SET-*` 码族；facet 上限 64/功能 |
| 07（V3-T8） | **零新增契约面**——`IFeatureLogger` 既有形状兑现矩阵行（每功能一律非 null）；消毒截断定值（64/128/256）、摘要容量 128、限频 30000ms；`BUE-LOG-*` 码族 |

**版本时序纪律**：「接上某 2.1 成员」≠「2.1 已发布」——01..08 期间一切构建=开发态内部基线（不产正式候选、不进 RELEASES、不授 CaseId）；对外 2.1 版本以 DEV-V3-09 整体候选经三环境验收+人工批准后为准（§C.5）。**翻转执行记录**：本附录成文时（DEV-V3-08）正文 §7 保持 2.0；**DEV-V3-09（2026-09-11）** 登记宿主支持契约 2.1 并经闭环翻转为对外发布 2.1。落地事实：候选 `ce0d2191…17e413` 经 SP/P2P/U3DS 三环境实机验收全判据通过 + **人工发布批准（2026-09-11，用户原话「批准发布！」）** → `audit/RELEASES.md` 行 11 当前发布物 = 本候选（Phase-3 平台缝 Minor 批次 T2..T8，破坏性变更零，2.0 模块三环境 `accepted=True` 回归绿）+ `publish/第3阶段-正式交付版本/` 交付包换新，v8 2.0 基线包退役归档。本 2.1 SDK 文档随该交付包对玩家生效（文档不独立发版，随主 DLL 契约版本走）。

**第二笔：DEV-V6-05/06 加性条目（同一 Minor 2.1 内的后续批次；V6-T1：本阶段契约仍 2.1，不另开 2.2）**

| 票 | 加性条目（全部向后兼容：既有 2.0 模块零重编译继续注册、继续运行；破坏性变更为零） |
|---|---|
| 05（V6-T5 Q1/Q2） | `IFeatureBootstrap.Patching`（第 **12** 成员，与 `MainThread` 同构的加性格子）；`IFeaturePatching` 恰一成员 `Register(IDisposable)` → `FeaturePatchRegistrationResult`（Registered/Reason/DiagnosticId/LifecycleGeneration）；`FeaturePatchRegistrationReason : byte` 值 `0..5` 冻结；**句柄=BCL `IDisposable`**（不新增契约句柄类型——与既有资源跟踪账同形）；经口受理的句柄进**既有**跟踪账（同 (FeatureId, 代际)、同逆序释放、同 64/代际，不新造第二套容器）；`BUE-PATCH-*` 码族（B.11）；可选面 `IFeaturePresentationRegistration`（`DisplayName` + `DirectPresentation`，**类型发现**式——`IFeatureRegistration` 成员一个未加）；面板目录行加性 `DisplayName`/`DirectPresentation`（功能自报值跨界；未自报者仍显示原始 FeatureId） |
| 06（V6-T6 Q5） | **公开摘要函数** `FeatureDefinitionDigest.ComputeArtifactPayloadDigest`（载荷 → `Digest256`）；受理路径改调它=单源同算法；§4 新增「登记工件」小段（`FeatureDefinitionArtifact` 的构造与摘要生成姿势；该类型是生产登记必经 DTO，**不是**未实装项） |

**本笔时序**：对外发布仍归本阶段唯一候选票（DEV-V6-10）走 §C.5 实施发布链；本笔期间的构建=开发态内部基线（不产候选、不进 RELEASES、不授 CaseId）。

### C.2 安全降级原则（旧模块在 2.1 宿主上）

1. **未知结果值不当成功**：`NetworkSendResult`（含新值 `Throttled`）与一切枚举/结构返回值——只有冻结的显式成功值算成功，未知值按失败/待重试降级（红测锚=04「未知 NetworkSendResult 旧模块安全降级」）。
2. **Bootstrap 成员 null 容忍**：阶段基线纪律（A.1）——面向未发布版本编码的模块对未接线成员 null 必须可降级；对 2.1 宿主按矩阵终态列即可免防御（五成员永非 null 是硬承诺）。
3. **显式结果不抛异常**：Admission/登记/投递/提交一律消费显式结果，不 try/catch 控制流；异常面只剩开发期错误（null-handler/null-task/未登记订阅 fail-fast）。
4. **拒绝按码分支**：B.1/B.3 拒绝码表的处置建议列即降级剧本（等待/禁用自身/提示缺前置/报版本不兼容）。
5. **诊断行不透明**：新码/新行出现不改变行为契约（摘要≠验收授权，导出归 UMM 人工）。

### C.3 Major 纪律（破坏性变更）

冻结面的破坏性变更**必升 Major** 并在正文 §7 登记迁移条目；宿主注册门槛=Major 恰 2（`ContractIncompatible` 拒其他 Major）。加性变更（新枚举值/新可选面/新构造器/新矩阵成员）走 Minor 批次，2.0 模块零重编译可继续注册。既有先例边界：正文 §2 程序集名冻结、§7 各条①-⑦ 破坏性登记格式不变。

### C.4 Contracts 拆分四条件=门禁条款（当前结论：**继续暂缓**，T9 裁决②）

第三方引用形态维持：**直接引用完整主 DLL `BetterUnturnedExperience.dll` + `CopyLocal=false` + 禁捆绑**（正文 §4）。不拆独立 Contracts.dll 的当前结论只对「四条件全部未触发」负责；逐条登记如下（定义/事实判定/触发信号/重评义务）：

| 条件 | 定义 | 当前事实判定（V3-R1 基线） | 触发信号（未来何时重看） | 触发后重评义务 |
|---|---|---|---|---|
| ① | 第三方需脱离完整 BUE DLL 编译 | **未触发**（NoOp 生态路径经主 DLL 编译+注册全程被验；R1 未发现真实第三方需求信号） | 出现「因引用完整 DLL 而放弃接入」的具名生态案例/仓库外构建约束 | 立案评估纯编译面子集；预判潜在最先触发=本条 |
| ② | 多仓库需要稳定纯契约包 | **未触发**（单仓库；契约文档随主 DLL 走） | 出现仓库外第二消费方需按包版本引用契约 | 评估包化+版本对齐纪律 |
| ③ | runtime 与 SDK 发布节奏须独立 | **未触发**（文档随主 DLL 版本，无独立发版需求） | 生态要求「契约先行、runtime 后补」的节奏分裂证据 | 评估独立发布通道；预判潜在第二触发=本条 |
| ④ | 需公开桥接 adapter 而不暴露主程序集 | **未触发**（注册桥 `BueRuntimeHost` 已在主程序集且面窄） | 出现必须藏主程序集内部面的具名安全/冲突案例 | 评估 adapter 程序集 |

触发预判（T9 裁决②具名）=**①编译脱耦**需求先现、**③发布节奏分化**次之；「可能先触发」≠「已触发」——本条只是排序预判。拆分实施动作不在本图（图级冻结）；任何拆分须另立票并满足四条件之一+重新裁决（spec Out of Scope「未满足四条件即拆 Contracts.dll」=永久冻结面）。

### C.5 SDK 文档版本绑定与 RELEASES 注记要求

- **文档不独立发版**：本 SDK 契约文档随 BUE 主 DLL 契约版本走、随实施发布节奏同步（v8 交付包内文档=2.0 基线原状保持；2.1 文档与本附录属开发态，对玩家生效随 09 交付包同步换新）。
- **实施发布链顺序**（T9 裁决⑦）：源码契约更新→SDK 文档同步→候选 DLL→实机/双轴审查→**SHA-256/CaseId 人工批准**→publish 交付包换新→RELEASES 行。
- **RELEASES 行注记最低要求**：契约版本（如 `2.1`）、候选 SHA-256、三环境验收绑定（LoadSetIdentity 轻链）、本附录各 C.1 条目批次的覆盖声明、破坏性变更指向正文 §7 登记条目。中间构建一律**不授** CaseId（01..08 候选纪律，本票同样遵守：不产候选 DLL、不更 RELEASES）。

### C.6 生态 DLL 上架前自检清单（11 项人工核对，T9 裁决④）

发布生态 DLL 前逐项打勾——本文不建自动验证工具（裁决①），清单=人工门禁：

- [ ] 1. BepInEx 插件入口存在：自有 `[BepInPlugin]` GUID，独立 DLL（不复用 BUE GUID、不请求源码聚合）
- [ ] 2. `[BepInDependency]` 前置指向 BUE GUID `io.github.yu80rice.betterunturnedexperience`，HardDependency
- [ ] 3. 编译期引用正确版本主 DLL，与目标 BUE 契约版本对齐（`MinimumBueContract`，A.1/C.3）
- [ ] 4. 引用 `CopyLocal=false`（`<Private>False`）
- [ ] 5. 发布包未捆绑 `BetterUnturnedExperience.dll`（只含你自己的 DLL）
- [ ] 6. FeatureId 不用官方保留段（`io.github.yu80rice.bue.*` 冒用=确定性拒 BUE-REG-010，B.10 示例对照）
- [ ] 7. DiagnosticId 不用 `BUE-*` 前缀（生态码=FeatureId 派生前缀，B.9；冒用=拒写留痕）
- [ ] 8. 对未知 `NetworkSendResult` 值安全降级（只有显式 `Sent` 算成功，C.2）
- [ ] 9. 不调用内部宿主控制面：AssemblyRef 无 `BetterUnturnedExperience.Contracts/Core`（正文 §4 闭包规则），只引用公开契约类型
- [ ] 10. 停止时释放事件/网络/Tick 资源：不自拆总线订阅（宿主自动注销）、处理停止边界写拒（BUE-SET-001/BUE-LOG-004/BUE-MT-002 为预期信号）
- [ ] 11. 不把 LogOutput、CaseId、RELEASES 当运行时契约（诊断=证据链非接口；摘要≠验收授权；发布台账人工）
