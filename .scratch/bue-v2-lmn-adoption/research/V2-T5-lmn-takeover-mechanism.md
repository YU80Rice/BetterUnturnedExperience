# V2-T5: 独立 LMN 共存接管机制查证

- 调查对象：当用户同时安装 BUE 与独立 LMN（LaunchMultiplayerNet）时，BUE 如何**检测**独立 LMN 实例并**停用**其运行——不删除 DLL、不误伤不相关插件、可诊断、可恢复。
- 调查性质：只读查证，无任何代码改动。
- 权威主源（均已直接核验，非二手）：
  - BepInEx 5.4.23.5 运行时程序集 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Libs\BepInEx.dll`（ilspycmd 10.1.1 反编译 `BepInEx.Bootstrap.Chainloader`、`BepInEx.BepInIncompatibility`、`BepInEx.BepInDependency`、`BepInEx.BaseUnityPlugin`）。
  - LMN 源码仓库 `D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`（V5）。
  - 消费方插件（V1/V2 生态）`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Archive\2-未闭环验证项目\{LaunchInventoryTidy,LaunchInPlaceReload,LaunchHordeTracker}`。
  - BUE 源码 `src\BetterUnturnedExperience.Plugin\BetterUnturnedExperiencePlugin.cs` / `BueNativeManagementPanel.cs`。
  - Harmony 2.9 / BepInEx 官方文档（execution / priorities / dependency-resolution）。

---

## 结论

### 推荐机制（分两步：检测 + 停用）

**检测**（无 plugins 目录扫描）：
- BUE 启动后的任一时刻，用静态注册表枚举已加载插件：`BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.yu80rice.launchmultiplayernet")`。
- 证据：`BepInEx.Bootstrap.Chainloader` 反编译 L45 `public static Dictionary<string, PluginInfo> PluginInfos { get; }` 以 GUID 为 key，L401 每次成功加载后写入 `PluginInfos[item6] = value2`，L402 同时写入 `value2.Instance = ...AddComponent(...)`。这是 BepInEx 运行时既有的**已加载插件注册表**，BUE 只读查它即可判断「独立 LMN 是否已加载」，**完全规避 plugins 目录扫描与 `Assembly.GetTypes()`**。
- 关键时序约束：`PluginInfos` 只在 `Chainloader.Start()` 执行到加载阶段（L395-404）才填充。BUE 需在**自己的 `Awake` 之后、网络可用之前**做检测。BUE 主插件 GUID `io.github.yu80rice.betterunturnedexperience` 字典序在 LMN GUID `com.yu80rice.launchmultiplayernet` 之后，BepInEx 对无依赖的独立插件按 `SortedDictionary<string,GUID>`（Chainloader L306）拓扑排序，故**本地 LMN 几乎总是先于 BUE 完成 `Awake`**（见「待格审项」的排序依赖）。因此检测与停用不得假设 BUE 先跑。

**停用**（推荐路径 b，优先权抢占接管；关键事实见「备选路径对比」）：
- BUE 自己的网络模块对共享拦截点 `NetMessages.ReceiveMessageFromClient` / `ReceiveMessageFromServer` 注册**最高优先权 Prefix**（`Priority.First`），返回语义与 LMN 的 Prefix 完全同构：命中 MOD/LMN2 帧 → 由 BUE 路由并 `return false`（跳过原方法与其余 Prefix）；未命中 → `return true`。
- 由于 Harmony 前缀按优先权降序执行、且任一 Prefix `return false` 即短路跳过后续所有 Prefix 与原方法，BUE 的 Prefix **先于 LMN 的 Prefix 执行**并接管 MOD/LMN2 帧，LMN 的 `ModRouter.TryHandle*` 永远不会被调用，其注册的处理器永不触发——实现「唯一网络 Hook + 唯一频道注册 + 唯一消息路由」，满足 CONTEXT「网络能力接管」L69-71 的 avoid「两个网络实现同时处理同一帧」。
- 对**不相关插件**零影响：BUE 的 Prefix 只对 MOD/LMN2 魔数帧短路，其余帧 `return true` 原路放行，其它插件自有拦截点不受影响（不误伤不相关插件，CONTEXT L70、L82）。

### 为什么「杀 Awake」（路径 a）不推荐
BUE 无法可靠地在 LMN `Awake` 执行之前 patch 它：加载顺序由 GUID 字典序（BepInEx SortedDictionary）决定，BUE 排在 LMN 之后；且 BUE 若强行给 LMN 类型加 `BepInDependency` 会造成 LMN 生态消费方的连带失效。见「备选路径对比 (a)」。

### 为什么「静态 BepInIncompatibility 属性」不可直接使用（关键红线）
BepInEx 提供 `[BepInIncompatibility(GUID)]`（Chainloader L330-343 在加载前把不兼容插件踢出 `pluginsByGUID`），看起来优雅。但消费方 LIT/LIR/LHT 都以 `[BepInDependency(com.yu80rice.launchmultiplayernet, HardDependency)]` 声明硬依赖（证据 LIT L18）。若 BUE 声明与 LMN 不兼容，BepInEx 会把 LMN 从加载列表移除，随后 LIT/LIR/LHT 因「missing dependencies」也被 **skip**（Chainloader L363-391）。这**直接误伤**尚未迁移的 LMN 生态插件，违反「不误伤不相关插件」。因此静态不兼容属性**不得**作为停用手段（除非作为明确的工程决策接受连坐，属待格审项）。

---

## 检测机制（BepInEx 运行时事实）

| 表面 | 证据（反编译 BepInEx.dll，5.4.23.5） |
|---|---|
| 已加载插件注册表（GUI→PluginInfo） | `Chainloader.PluginInfos` 声明 L45；写入 L401 `PluginInfos[item6] = value2` |
| 每插件实例引用 | L402 `value2.Instance = (BaseUnityPlugin)ManagerObject.AddComponent(...)`；`PluginInfo.Instance` 由此填充 |
| 每插件来源/类型 | L399 `Assembly.LoadFile(value2.Location)`；L402 `value4.GetType(value2.TypeName)`；`PluginInfo.{Location,TypeName,Metadata}` |
| 宿主 GameObject | L104 `public static GameObject ManagerObject`；L289 `ManagerObject = new GameObject("BepInEx_Manager")` |
| 插件自身信息 | `BaseUnityPlugin` 反编译：`public PluginInfo Info { get; }` |
| 版本/名称 | `PluginInfo.Metadata`（BepInPlugin），`bepInPlugin.GUID` 校验于 L202-206 |

**为何这是合规的检测面**：读 `Chainloader.PluginInfos` 只枚举**已经装入进程、已实例化**的插件，不触目录不触反射枚举类型，天然满足「不扫描插件目录 / 不用 Assembly.GetTypes()」硬规则（V2-T5 问题段约束）。且它拿到的就是**运行时实例**而非静态盘点——即使 LMN DLL 在目录里存在但被 BepInEx 跳过（如不兼容/版本不符/进程过滤 L319-324），`PluginInfos` 里没有该 GUID，BUE 不会误报。

**检测时机**：BUE `Awake` 无法保证晚于 LMN `Awake`（见排序），故应在 BUE 自己的网络模块首次初始化/挂网时检测；检测只回答「本次会话是否已加载独立 LMN」，是**会话级**事实，随 BepInEx 一并起停。

---

## LMN 的入口与「有用/有害工作」定位

LMN 全部网络工作由 `LaunchMultiplayerNetPlugin` 这一处驱动（`Core\LaunchMultiplayerNetPlugin.cs`）：

| 位置 | 动作 | 行号 |
|---|---|---|
| `Awake()` | `DontDestroyOnLoad`；`ModTransport.Initialize()`（净反射初始化 + ConnectionSessionManager + NamespacedTransport）；创建 Harmony `com.yu80rice.launchmultiplayernet`；`NetMessagesReceiveClientPatch.Apply` / `NetMessagesReceiveServerPatch.Apply`；patch 自检失败→`ModTransport.IsOperational=false` 且 `this.enabled=false` fail-fast；成功→`IsOperational=true` | L50-74 |
| `Update()` | 每帧 `ModTransport.Poll()`（暂存队列清理/超时） | L76-80 |
| `OnDestroy()` | `_harmony.UnpatchSelf()`；`ModTransport.Shutdown()` | L82-86 |

- **入口身份**：`[BepInPlugin("com.yu80rice.launchmultiplayernet", "LaunchMultiplayerNet", "5.0.0.0")]`（L41,44-45），对齐预埋事实 1。
- **拦截点（唯一有害面）**：`Patch\NetMessagesReceiveClientPatch.cs:56` `Prefix(ITransportConnection, byte[], int, int)` 拦 `NetMessages.ReceiveMessageFromClient`（U3-SDK `NetMessages.cs:123`）；`Patch\NetMessagesReceiveServerPatch.cs:55` `Prefix(byte[], int, int)` 拦 `ReceiveMessageFromServer`（U3-SDK L167）。两者都：未命中→`return true`（放行 vanilla）；命中 MOD/LMN2→`ModRouter.TryHandle*` 返回 true→`return false` 短路。
- **服务端补丁守卫** `!Provider.isServer → return true`（ClientPatch L58）；**客户端补丁守卫** `Provider.isServer → return true`（ServerPatch L57）——两个方向天然互斥，同一帧只由一端处理。
- **会残留但默认无害的状态**：即便 BUE 接管，LMN 的 `Awake` 仍已跑完 → `ModTransport` 静态表、`ConnectionSessionManager`、`NamespacedTransport` 已初始化，`IsOperational=true`。这些状态在 BUE Prefix 短路后**永不触发**，仅占用内存/无业务副作用。要彻底静默它，唯一途径是让 LMN 的 `Awake` 不执行或让其 `IsOperational=false`（都需要在 LMN Awake 之前介入——路径 a/c 的难点所在）。

---

## 备选路径对比

| 路径 | 做法 | 可行性 | 约束裁决 |
|---|---|---|---|
| **(a) Harmony patch LMN 的 Awake/Start，使其短路由** | BUE 用 `AccessTools.TypeByName("LaunchMultiplayerNet.LaunchMultiplayerNetPlugin")` 定位类型并 patch 其 `Awake` 为 no-op | **不可靠**：①加载顺序不确定且大概率 LMN 先跑（BUE GUID 在 LMN 之后，Chainloader SortedDictionary 顺序），patch 一个已执行过的 `Awake` 无效；②LMN 类型是 BUE 运行时可访问的（同进程、程序集已 LoadFile），反射 get 可行，但 patch 时机无法保证；③patch 目标是另一插件的私有方法，脆弱且属对其它插件的强耦合 | 不满足「不误伤/可恢复」（破坏其它插件运行受 BUE 停用路径绑架） |
| **(b) BUE 自身更高优先权 Prefix 全覆盖接管** | BUE 网络模块对 `ReceiveMessageFromClient/Server` 注册 `Priority.First` Prefix，语义与 LMN 同构，命中 MOD/LMN2 帧即 `return false` 短路 | **首选（推荐）**：Harmony 前缀按优先权降序、先返回 false 者短路后续（Harmony execution 文档）；LMN Prefix 永不再被调用，其处理器永不触发。不相关插件帧 `return true` 原路放行，不受影响。可诊断（BUE 面板报「已接管」）、可恢复（移除 BUE 后 LMN 原样 work） | **满足全部约束**。核心：唯一网络 Hook/频道注册/消息路由（CONTEXT L70）；不删除文件（L82）；不误伤不相关插件（L70,L82） |
| **(c) 运行时标记/停用标志（BUE 设置某标志，LMN 检查）** | BUE 写一个标志，期望 LMN 识别 | **不可行（原样）**：LMN 源码没有任何外部可写标志的钩子；它的启停完全由自身 `Awake` 与 `IsOperational` 决定。要让 LMN 检查某标志=仍需 patch LMN=收敛到 (a) 的不可靠性 | 不成立，除非搭配 patch（退化到 a） |
| **(d) BepInEx Chainloader 级运行时卸载另一插件** | 程序内调用卸载 API | **不可行**：BepInEx 5 不支持运行时卸载插件。Chainloader `Start()` 无 teardown 路径，插件作为 `ManagerObject` 上的组件被 `AddComponent`（L402），没有公开 Remove/Destroy-Plugin API；`PluginInfos` 只增不减（除加载异常 L408）。「完整卸载」在 BepInEx 5 语义上不存在 | 文档化结论：停用必须走 patch/优先权，而非卸载 |

**排序（Rank）**：因此唯一通行的停用路径是 **(b) 优先权抢占接管**。其「诊断/恢复」天然最强：不用动 LMN 文件、不用永久改任何状态，BUE 一卸，下个会话 LMN 按自身逻辑独立运行。

### (b) 的内部细节与边界（格审重点）
- **Harmony 语义核验**：Harmony 执行流——前缀按 `[HarmonyPriority]`/`HarmonyMethod.priority` 降序执行；某前缀返回 `false` 时，后续前缀与原方法**不再执行**（execution doc）。LMN 未设优先级=默认 `Priority.Normal`，BUE 用 `Priority.First` 确保先执行。
- **象限完整**：BUE 必须对**全部** MOD/LMN2 帧接管（V1 legacy + V2 named 均由 BUE 网络模块路由），否则 LMN 仍会兜住没被 BUE 处理的 MOD/LMN2 帧（double-handling）。这是 T3/T4 交合点：BUE 网络模块要覆盖 LMN V1 兼容路径（T4）与 V2 命名频道（T3），正是两票已定的方向。
- **共享状态残响**：LMN `Awake` 仍初始化其静态 `IsOperational=true`；若存在仍绑定 LMN 静态 API 的消费方（LIT 等 `RegisterNamedServerHandler/SendNamedToServer`），它们的注册写进 LMN 静态表但 BUE Prefix 短路永不触发——**对这些消费方而言是「看似在用实则被接管」**。它们的迁移属于 T4/V1 兼容策略的治理范围，不是本票能单独判定。这是「buying接管」与「生态迁移」的界限问题，列入待格审项。

---

## 诊断 / 恢复

**诊断（管理面板）**：BUE 已有原生管理面板（`BetterUnturnedExperiencePlugin.cs:70` `BueNativeManagementPanel`；`BueNativeManagementPanel.cs` `FeatureId = "io.github.yu80rice.bue.management-panel"`，运行时模型 `BueManagementPanelRuntime`）。BUE 检测到 `PluginInfos.ContainsKey(LMN_GUID)` 后，向面板模型注入一条状态条目，正文「已由 BUE 接管」，并记录接管事实到 BUE 结构化日志（复用现有 `BueRuntimeLog.Runtime` / `diagnosticId` 惯例）。满足 CONTEXT「网络接管用户提示」L97-99：不弹阻塞启动的确认窗、保留日志。

**恢复（可恢复性）**：
- BUE/Patch 全部随 BepInEx 会话生命周期走。移除 BUE 后（删/关 BUE DLL 或禁用其网络模块），下个会话不注册 Priority.First Prefix，LMN 按自身逻辑独立运行，**无需任何修复动作**。
- (b) 路径对 LMN 侧**零持久状态改动**：LMN 文件不动、配置不动、静态表只在内存且随会话销毁。完全满足 L82「用户确认后可自行清理旧文件」。
- 严格说「恢复」= 移除 BUE（或禁用网络模块）+ 让 LMN 回到自带状态，是**可逆的升降级**，不是运行时热切换。

---

## 待格审项（开放决策，供格审轮）

1. **优先权策略的边界完整性**：BUE 的 Priority.First Prefix 是否必须覆盖 V1 legacy + V2 named 全部帧，才承诺「唯一路由」？若只承诺 V2、把 V1 交给 LMN，则与「唯一网络 Hook」冲突——需明确接管覆盖面与 T3/T4 的切割。
2. **patch-vs-priority（a vs b）的取舍**：本报告判 b 唯一可行（因加载顺序不可控）。格审应确认是否接受「LMN Awake 仍执行、静态表仍初始化、但路由被 BUE 短路」这一残响语义；若要求「彻底停用 LMN」，则必须接受对 LMN 老化不可靠性的 patch 代价，需在格审里划清。
3. **静态 BepInIncompatibility 的连坐风险**：是否接受「BUE 与 LMN 互斥 → LIT/LIR/LHT 等硬依赖 LMN 的未迁插件一并 skip」？本报告明确禁止（误伤不相关插件），但需格审确认——尤其是否以「LMN 生态已全迁 V2（T8 100%）」为由接受连坐，还是坚决走运行时优先权。
4. **面板 UX**：「已由 BUE 接管」应该做成一条只读状态 + 链接到「配置迁移」（T6）卡，还是带「让我改回独立 LMN」的停药按钮（可逆性展示）？按钮语义需与「网络模块可关（T7）」对齐。
5. **检测的精确性**：`PluginInfos.ContainsKey(GUID)` 只反映「已加载实例」。是否需要区分「DLL 存在但被 BepInEx 跳过（如版本/进程不符）」与「已真正运行」？当前推荐只处理「已加载」，避免误报，需格审确认此口径。
6. **排序依赖**：BepInEx 对无依赖独立插件的加载顺序随 `SortedDictionary<GUID>` 稳定但 BUE 排在 LMN 之后。若要 BUE 先于 LMN 接管，是否接受为 BUE 声明一条隐式排序手段（如软依赖 `BepInDependency(LMN_GUID, SoftDependency)`）？这会把 BUE 与 LMN 绑进同一依赖图、可能在 LMN 缺省时改变 BUE 行为，需格审权衡。

---

## 证据清单（文件 + 行号）

### BepInEx.dll（5.4.23.5，ilspycmd 反编译）
- `BepInEx.Bootstrap.Chainloader`：L45 `PluginInfos` 声明；L102 `DependencyErrors`；L104 `ManagerObject`；L289/294 宿主 GameObject；L295 `TypeLoader.FindPluginTypes(Paths.PluginPath,...)`（**这行说明发现确按 plugins 目录，但那是 BepInEx 自身，BUE 只是消费其注册表结果**）；L306 `SortedDictionary<string,...>(StringComparer.InvariantCultureIgnoreCase)`（排序）；L330-343 不兼容剔除；L363-391 依赖缺失 skip；L395-419 加载循环（L399 `LoadFile`、L401 填 `PluginInfos`、L402 `AddComponent`→实例、L408 异常移除）。
- `BepInEx.BepInIncompatibility`：L10-18 `AttributeUsage(Class, AllowMultiple)`，`IncompatibilityGUID`。
- `BepInEx.BepInDependency`：L13-18 `DependencyFlags{Hard=1,Soft=2}`；L26 ctor。
- `BepInEx.BaseUnityPlugin`：`public PluginInfo Info { get; }`；`protected ManualLogSource Logger`；`public ConfigFile Config`。

### LMN 源码
- `Core\LaunchMultiplayerNetPlugin.cs`：L41 GUID 属性；L44 `Guid="com.yu80rice.launchmultiplayernet"`；L45 `Version`；L50-74 `Awake`（L57 `ModTransport.Initialize()`、L59-62 patch apply、L64-70 自检 fail-fast、L72 `IsOperational=true`）；L76-80 `Update`→`ModTransport.Poll()`；L82-86 `OnDestroy`→`UnpatchSelf+Shutdown`。
- `Patches\NetMessagesReceiveClientPatch.cs`：L27 反射绑 `SDG.Unturned.NetMessages.ReceiveMessageFromClient`；L56 Prefix 签名；L58 `!Provider.isServer→true`；L62-64 命中→`return false`。
- `Patches\NetMessagesReceiveServerPatch.cs`：L26 绑 `ReceiveMessageFromServer`；L55 Prefix；L57 `Provider.isServer→true`；L61-63 命中→`return false`。
- `Routing\ModTransport.cs`：L45 `IsOperational`（internal set）；L93-106 `Initialize()`（NetReflectionHelper/SessionManager/NamespacedTransport）；L137-179 注册 handler；L186-239 注销。
- `Routing\ModRouter.cs`：L13-24 魔数 `MOD`/`LMN2` 常量；L26-31 `TryHandleFromClient`；L33-38 `TryHandleFromServer`。

### 消费方（生态）
- `Archive\...\LaunchInventoryTidy\LaunchInventoryTidyPlugin.cs` L18 `[BepInDependency(LaunchMultiplayerNetPlugin.Guid, HardDependency)]`。
- LIT/LIR/LHT 各 `*.cs` 直接调用 LMN 静态 API：`RegisterNamedServerHandler/RegisterNamedClientHandler/SendNamedToServer/SendNamedToClient`（`ManualTidyNetwork.cs:152-153,473,1100`；`AmmoRepackNetwork.cs:381-382,456`；`HordeStatusNetwork.cs:70,97,323`）。

### U3-SDK 拦截点（T1 已证，此处沿用）
- `Assets\Runtime\Assembly-CSharp\NetMessaging\NetMessages.cs` L123 `ReceiveMessageFromClient`（服务器每包入口）；L167 `ReceiveMessageFromServer`（客户端入口）。

### BUE
- `src\BetterUnturnedExperience.Plugin\BetterUnturnedExperiencePlugin.cs` L12 BUE GUID `io.github.yu80rice.betterunturnedexperience`（排序在 LMN 后）；L70/72 管理面板初始化；L51 `BueRuntimeLog.Bind`。
- `src\BetterUnturnedExperience.Plugin\BueNativeManagementPanel.cs` L74 `PluginId`、L75 `FeatureId="io.github.yu80rice.bue.management-panel"`、L134-144 构造、L161 Initialize、L170 Dispatch。

### 官方文档（行为佐证，非本项目源码）
- Harmony execution / priorities：https://harmony.pardeike.net/articles/execution.html 、https://harmony.pardeike.net/articles/priorities.html
- BepInEx dependency/incompatibility 语义：https://deepwiki.com/BepInEx/BepInEx/3.1.2-dependencies-and-incompatibilities
- BepInEx 5 不支持运行时卸载插件（Chainloader 无 teardown 路径；插件作为 ManagerObject 组件生命周期承载）：Chainloader 反编译 L289/L402（本项目自有证据），可结合 BepInEx 社区长期共识核对。

---

## 附录 — 调查方法
- BepInEx.dll（仓库 `Libs\`，5.4.23.5）用 `ilspycmd -t <type>` 反编译导出到 `C:\Windows\Temp\dsh-t5-*`（会话临时产物，非交付物）。
- LMN / 消费方 / BUE 源码直接读取；U3-SDK 沿用 T1 已固证的拦截点行号（本票未重查 U3-SDK，仅引用 T1 结论）。
- 纯只读查证，未改动任何源码；未关闭 ticket。