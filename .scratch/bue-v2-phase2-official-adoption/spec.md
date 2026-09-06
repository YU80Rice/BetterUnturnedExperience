# V2 第二阶段规格：三插件官方纳入与平台首公里

Type: spec
Status: ready-for-agent
Parent: V2 开放运行时平台方向（CONTEXT.md 已冻结的 BUE V2 产品方向）；承接第一阶段（LMN 官方纳入与 BueNetworkApi，全链闭环）
Source: wayfinder 地图 `.scratch/bue-v2-phase2-official-adoption/map.md`（7/7 resolved，2026-09-06）；T3–T7 五张决策票 Answer；T1/T2 research 报告；DEV-V2-08 结单与 kit
Author: GPT（/to-spec 合成；决策经人工 grilling 拍板；票内具名移交到 /to-spec 的两处定稿项见「网络模块停用语义」与「功能事件落位」两节）

---

## Problem Statement

第一阶段完成后，BUE 已有 `BueNetworkApi` 契约与官方网络模块，但该 API **没有生产传输绑定、没有真实消费者**（08 R5 勘误实锤）。Launch 系列三个功能——背包整理、更好的换弹体验、更好的尸潮播报——仍是独立 BepInEx 插件：玩家要装 BUE + LMN + 三插件共五个 DLL；三插件全部硬依赖 LMN 并直调其内部 `ModTransport` 门面；管理面板没有官方中文名；第三方开发者没有任何「我引用 BUE 前置时什么身份是稳定的」的契约文档，误拷 DLL 双装时只会得到静默异常。

玩家侧应当只部署一个 `BetterUnturnedExperience.dll`，四个官方功能在单人 / SteamP2PFriends / U3DS 三环境全部可用；`BueNetworkApi` 应当有生产传输绑定，三插件应当成为它的第一批真实消费者；第三方开发者应当有明确的三段式契约与结构化的双装诊断。

## Solution

沿第一阶段「吃掉并消化」的同一形态完成第二阶段：

1. **平台**：`BueNetworkApi` 升契约——入站订阅（带方向枚举）、`IFeatureBootstrap.Network` 注入、`PartialFailure` 发送结果、会话快照收窄为 established、网络模块自动握手、发送语义改为会话驱动组播；BUE 帧消费 seam 与 LMN 接管 seam 拆分；线帧魔数 BUE2 → BUE1。
2. **三插件纳入**：源码迁入本仓库，以 EmbeddedOfficial 形态聚合进单一玩家 DLL；各自成为 `IFeatureModule` 官方功能模块，FeatureId 重置为 `io.github.yu80rice.bue.*`，显示名用四件官方中文名；网络调用全部改走 `IBueNetworkApi` 命名频道；删除 BepInEx 插件身份与 LMN 硬依赖。
3. **稳定 seam**：LIT 立整理策略接口（算法原样迁移）；LIR 立换弹上下文守卫与换弹动作 adapter；LHT 立追踪策略与表现 adapter（内部双件）；跨功能协作走功能事件（TidyCompleted）与宿主时钟（HostTick），两者均入公开契约。
4. **平台首公里**：开发者契约三段式文档（承诺 / 不承诺 / 编译期指引）扩写进现有 SDK 文档；防双装为注入式运行时自检诊断（`BUE-PLATFORM-001`），不宣传为完整防重复加载系统。

到达标准（目的地验收）：裸 BUE（无 LMN、无三插件 DLL）时四个官方功能三环境全部可用；面板显示官方中文名；三插件是 BueNetworkApi 生产绑定的第一批真实消费者；未知 V1 旧插件的共存承诺不破坏（裸 BUE 无独立 LMN 的环境除外，为已承认边界）。

## User Stories

1. 作为玩家，我希望只装一个 `BetterUnturnedExperience.dll` 就能获得更好的物品交互、背包整理、更好的换弹体验、更好的尸潮播报四个功能，以便不再寻找和拼装多个独立 DLL。
2. 作为玩家，我希望这四个功能在单人、SteamP2PFriends 本地联机、U3DS 专用服务器三种环境下都可用，以便任何游玩方式体验一致。
3. 作为玩家，我希望在管理面板看到四个官方功能的中文名（更好的物品交互 / 背包整理 / 更好的换弹体验 / 更好的尸潮播报），以便一眼认出功能。
4. 作为玩家，我希望收藏条目在面板改版后不丢失，以便长期使用中我的偏好稳定（条目身份 = FeatureId，与显示名无关）。
5. 作为玩家，我希望每个官方功能都可以在面板单独关闭，以便不需要的能力不产生任何行为。
6. 作为玩家，我希望关闭某功能后回到原版行为（原生回退），之后可再次开启，以便关闭不等于卸载或损坏。
7. 作为单人玩家，我希望背包整理在本地直接完成整理事务，以便无需联机也能使用。
8. 作为 P2P 客机玩家，我希望整理请求可靠地发往主机并由主机权威执行，以便与主机看到的物品一致。
9. 作为 P2P 主机玩家，我希望自己的整理请求走本地路径而不自发自收，以便不会出现重复处理。
10. 作为玩家，我希望一次 P2P 会话中触发的整理熔断不污染下一次连接（熔断作用域绑定本次连接代际），以便坏连接的影响随断线结束。
11. 作为玩家，我希望熔断历史在磁盘上跨会话保留，以便反复故障的对象持续被记录（功能私有 JSON 权威不变）。
12. 作为玩家，我希望双击换弹键触发原位压弹，行为与旧独立插件一致，以便升级不改手感。
13. 作为玩家，我希望背包整理完成后自动压弹，无需新增开关，以便行为迁移不夹带行为重设计。
14. 作为玩家，我用更好的物品交互拖入物品时，不希望误触发换弹逻辑，以便两个功能叠加不互相干扰。
15. 作为玩家，我关闭更好的换弹体验时，希望只撤销它自己的游戏补丁，以便不影响更好的物品交互或其它功能。
16. 作为 U3DS 服务器管理员，我希望尸潮追踪与播报在无头服务器上正常追踪并广播（表现状态 HeadlessOnly 不阻塞功能验收），以便无 UI 环境不扣功能分。
17. 作为 P2P 主机玩家，我希望尸潮快照发给每个已连接的 BUE 客户端，但不给自己的本地 HUD 回发一份，以便主机界面不重复。
18. 作为客户端玩家，我希望以 10Hz 的稳定节奏刷新尸潮 HUD，以便播报平滑且不卡顿。
19. 作为玩家，我希望 `/horde` 命令仍由原版权限系统守门，以便服务器权限不被插件复制或绕过。
20. 作为玩家，我关闭更好的尸潮播报时，希望服务器停追踪停广播、客户端停 HUD、`/horde` 不再产生该功能行为，以便关闭即完整停摆。
21. 作为未装 BUE 的原版玩家，我希望连接装有 BUE 的服务器时不被任何功能帧打扰，以便原版体验零感知。
22. 作为旧插件用户（依赖 LMN V1 数字频道的未知插件），我希望在「BUE + 独立 LMN」部署下旧插件继续收发，以便共存承诺不破坏（裸 BUE 无独立 LMN 除外，为已承认边界）。
23. 作为功能开发者，我希望通过 `Subscribe` 按频道与入站方向登记收帧回调，以便只凭公开契约就能接收帧。
24. 作为功能开发者，我希望方向枚举表示「入站帧来自客户端 / 来自服务器」而不是我的本地角色，以便监听主机双角色时不混淆。
25. 作为功能开发者，我希望从 `IFeatureBootstrap.Network` 拿到网络 API 且它永不为 null，以便生命周期内任何时刻调用都有显式结果。
26. 作为功能开发者，我希望网络模块停用时订阅仍合法、入站为零、发送返回显式枚举，以便我能据此降级而不是猜。
27. 作为功能开发者，我希望组播只发给已建立会话的对端并返回部分失败结果（`PartialFailure`），以便不必解析日志就能感知部分失败。
28. 作为功能开发者，我希望会话建立（握手、断线清理、重连换代际、重复握手去重）由网络模块自动完成，以便功能模块零握手负担。
29. 作为功能开发者，我希望公开会话集合只含已建立会话，以便永远拿不到不可发送的 pending 会话。
30. 作为功能开发者，我希望单个回调异常不扩散到其它订阅、回调不在状态锁内执行，以便一个坏消费者不拖垮网络。
31. 作为功能开发者（生态），我希望与官方功能使用完全相同的网络、时钟与功能事件契约，以便不存在「官方私有通道」。
32. 作为功能开发者，我希望订阅宿主时钟获得帧级驱动（只含序号、时间、阶段），以便不必自建 Unity Update 泵。
33. 作为功能开发者，我希望整理完成事件（TidyCompleted）在公开契约上，以便第三方也能消费而无需 Harmony postfix 挂进官方功能。
34. 作为第三方插件开发者，我希望 BUE 承诺插件 GUID 与程序集名冻结、公开契约按版本演化，以便版本更新改文件名不破坏我的前置引用。
35. 作为第三方插件开发者，我希望编译期指引明确（引用主 DLL、CopyLocal=false、禁止捆绑 BUE DLL 进发布包），以便不制造双装。
36. 作为玩家，我误把 BUE DLL 拷进另一个插件的发布目录时，希望看到结构化诊断（诊断 id、双方路径、移除建议），以便自行修复而不是面对静默异常。
37. 作为玩家，我希望 BUE 永不自动删除我的文件，以便误判时的损失可控。
38. 作为维护者，我希望双装自检可以在不触文件系统的前提下红测（注入程序集列表），以便验收与实机解耦。
39. 作为维护者，我希望三插件源码只有本仓库一个事实源、原 Archive 仓库停维护，以便不再有双实现漂移。
40. 作为维护者，我希望旧实机测试夹具（TIDY_TEST_HARNESS 等）不进玩家 DLL，以便发布物不携带探针与测试代码。
41. 作为维护者，我希望各功能的 Harmony ID 收编到各自 FeatureId 下、Stop 只撤销自己，以便功能间补丁互不误伤。
42. 作为发布维护者，我希望契约冻结面变更随本次契约版本升级登记，以便第三方能对照版本文档升级。

## Implementation Decisions

### 决策来源

全部来自 wayfinder grilling（人工逐条拍板）+ T1/T2 research 查证；证据见 `.scratch/bue-v2-phase2-official-adoption/research/`。08 kit（三插件在「BUE 接管态 + 独立 LMN DLL」下三环境收发）为行为基线，官方纳入终态（裸 BUE）是本阶段闭合的缺口。所有实施票按 output-review-loop 纪律执行：红测先行 → 双轴独立审查（standards-reviewer / Spec-Reviewer 专属智能体）→ CLEAN 才交付；未公开分发，延续每票候选 + RELEASES 加行节奏。

### 平台：入站订阅与方向语义（T3 决策 1）

- 契约新增（签名来自决策票，实施期落 Contracts 冻结面）：

```csharp
IDisposable Subscribe(FeatureId channel, ChannelDirection direction, Action<IConnectionSession, byte[]> handler);
enum ChannelDirection : byte { FromClients, FromServer }
```

- 方向 = 入站帧来源，不是本地角色；同频道可分别订阅两方向。
- 冻结语义：每次订阅返回独立幂等的句柄，Dispose 只注销自己的委托；handler 锁外执行；单 handler 异常不破坏其他订阅；空 handler / 非法方向属开发者错误，参数异常 fail-fast；订阅未注册频道属合法（handler 表与频道注册解耦，帧仅在频道注册且流量到达后派发）。
- 运行时内部必须同步改为按方向的双 handler 表——只改签名不改 seam 视为未完成。

### 平台：功能获取网络入口（T3 决策 2）

- `IFeatureBootstrap` 新增 `Network` 属性携带 `IBueNetworkApi`；官方与生态功能同一公开契约，测试注入假 adapter；生命周期绑 `IFeatureBootstrap` / `IFeatureLifetime`；不泄漏 Host / LMN / Unity 类型。
- 冻结要求：`Network` 永非 null；网络模块未就绪时方法返回显式结果（绝不 null / 静默异常）；功能停止后订阅句柄与频道注册失效或可安全重复释放；属 Contracts 冻结面变更，升契约版本。

### 平台：网络模块停用时的方法级语义（T3 具名移交，本规格定稿）

网络模块停用不是独立错误码，而是既有冻结语义的自然投影：

- `Network` 仍返回同一非 null API 实例；`RegisterChannel` / `UnregisterChannel` / `Subscribe` 正常工作（频道表、订阅表与网络运行解耦，与「订阅未注册频道合法」同构）。
- 入站派发自然为零（无会话即无帧）；`Sessions` 为空快照；`Connected` / `Disconnected` / `GenerationChanged` 不触发。
- 所有发送按既有枚举返回：会话快照空 → `NoSession`（不新增「模块停用」专用码，不引入 null / 静默异常）；功能模块以发送结果与会话快照作为状态查询手段，不新增专用查询面。
- 网络模块重新启用后按自动握手重建会话并更换代际，已挂订阅无需重挂。

### 平台：BUE 帧消费 seam 与 LMN 接管 seam 拆分（T3 决策 3）

- 旧规则改写：探针 false → **零 LMN 相关 patch / 反射 / 镜像**（不再说「零 patch」——BUE 帧消费需要 BUE 自己的 patch）；patch 安装门 = 网络模块启用，与 LMN 探针解耦。
- 前缀决策顺序冻结：① 识别 BUE 帧 → ② 网络模块开 → BUE 消费 → ③ 识别 MOD/LMN2 → ④ LMN live → 放行原生（LMN 自己派发）→ ⑤ LMN inert → BUE 处理 → ⑥ 非目标帧交还 vanilla。
- 契约不变性：网络关闭时不消费 BUE 帧且 patch 被移除；BUE 帧与 MOD/LMN2 不误分类；LMN live 时每帧恰一次派发；探针 false 零 LMN 反射 / 镜像；live/inert 每方向独立判断；异常路径 hand-back 不吞原生流量。
- 两个 seam（BUE 帧消费 / LMN 接管）不得共用一个 takeover 布尔。

### 平台：发送语义 = 会话驱动组播（T3 决策 4）

- `SendToClients` = 向当前已建立 BUE 会话逐一定向发送，不是无目标帧交底层广播；无会话的原版玩家不收；本地主机天然不在远端会话集合（功能模块无需手写跳过本地身份）。
- `SendToClient` 维持按 `IConnectionSession` 寻址，**不新增** SteamId 重载；校验 session 归属本运行时、established、当前连接代际。
- 冻结发送结果语义：快照空 → `NoSession`；≥1 成功 → `Sent`；有目标全失败 → `LocalTransportUnavailable`；**新增 `PartialFailure`** 枚举成员表达部分失败。发送不持状态锁。

### 平台：会话建立 = 网络模块自动握手（T3 决策 5）

- 流程：transport connected → 运行时发 Hello → 服务器 Ack/Reject → Connected → 功能收发；功能模块零握手负担。
- 运行时职责：Hello/Ack/Reject、断线清理、重连新会话身份与代际、超时重探退避、重复 Hello 去重、版本不兼容 fail-closed、无 ghost session。
- 契约只公开 `Sessions`、`Connected`、`Disconnected`、`GenerationChanged`、发送结果；pending 会话仅内部可见；Ack 按 peer + 代际 / nonce 匹配（废弃「第一个未建立会话」）；`Connected` 仅握手完成后触发；Connected / Disconnected 锁外执行。

### 平台：线帧魔数与频道政策（T3 决策 6）

- 魔数常量 BUE2 → BUE1（4 字节帧头布局不变）；帧从未上线，改名零兼容代价。
- 产品与文档语言一律「BUE 帧」；"BUE1/BUE2" 只允许出现在魔数常量与格式换代文档；实施票清扫全部 "BUE2" 字样（常量、注释、测试、文档）。
- 数字频道政策重申：数字频道（0..255）只保留既有旧插件的兼容接收 / 运行，**不提供新注册入口**；三官方功能禁止向 V1 兼容层登记任何频道。

### 平台：契约变更清单与落地顺序（T3）

- 冻结面变更五项：① `Subscribe` + `ChannelDirection`；② `IFeatureBootstrap.Network`；③ `NetworkSendResult` + `PartialFailure`；④ `Sessions` 语义收窄为 established 快照；⑤ 契约版本升级（破坏性变更升 Major）+ 冻结面变更登记。功能注册的 `MinimumBueContract` 对齐新版本。
- 运行时内部改造：方向双 handler 表、会话驱动组播、自动握手、pending 隐藏、锁外回调。
- 落地顺序（用户定）：订阅入口 → Network 注入 → 发送语义 → 自动握手 → 帧消费 / 接管拆分（先给功能模块稳定 leverage，再收会话与传输复杂度进运行时）。

### 三插件迁入形态（T2 拍板 + T4/T5/T6 落实）

- 形态 = 与现有 EmbeddedCore / EmbeddedClientUi 同构：三个领域项目源码迁入 `src/`，由 Plugin 工程 Compile Include 聚合进单一 `BetterUnturnedExperience.dll`；各做 `IFeatureRegistration` + `IFeatureModuleFactory` + `IFeatureModule`，经宿主注册面进入，无私有通道。不产出任何独立 BepInEx 插件 DLL；不引 LMN DLL。
- 必须删除 / 改写：`[BepInPlugin]` / `[BepInDependency(LMN,Hard)]` / `BaseUnityPlugin` 入口；LIT 的 `LmnDependencyGuard`、LHT 的 `RuntimeDependencyGuard`（LMN ABI 守卫）；LHT 对 `ModTransport.Initialize()` 的显式调用（三件唯一调用者，传输初始化上收 BUE 网络模块：网络模块初始化传输、建立会话、提供 API，功能只注册频道 / 订阅会话 / 发送）；全部 `ModTransport.*` 调用改为 `IBueNetworkApi`。
- 保留为功能私有（不进契约）：负载字节布局与版本封套、LIT 会话 challenge / 账本 / 玩家操作表、排列算法、Harmony 对游戏类型的补丁（Start 装 / Stop UnpatchSelf，Harmony ID 收编到各自 FeatureId 下）、LIR 注册延迟到首帧游戏线程 + 半注册回滚、LIR 主线程 dispatcher 队列、LHT epoch/seq/单槽 mailbox/脏标记/双可靠度/停止闸门。
- 静态表（玩家表、会话表、熔断表、队列）全部绑到 `IFeatureModule.Start/Stop` 的功能代际，禁止跨功能泄漏；LIT 三阶段卸载（静默 → dispatcher 关停 → 完全关停）映射到 `IFeatureModule.Stop`。
- `BuildNamedMessage` 类纯组包 helper 改为功能内自组 `byte[]`，不占契约。

### 功能身份与显示名

- FeatureId：LIT = `io.github.yu80rice.bue.inventory-tidy`；LIR = `io.github.yu80rice.bue.in-place-reload`；LHT = `io.github.yu80rice.bue.horde-tracker`（BII 沿用 `io.github.yu80rice.bue.better-item-interaction`）。
- 规则：面板条目身份 = FeatureId = 网络频道身份，一词一贯；不沿用旧独立插件的频道字符串（那是旧传输实现身份，历史帧与新 BUE 帧本不兼容，沿用零收益）；旧命名频道随原插件退役。
- 显示名（固定中文，非身份）：更好的物品交互 / 背包整理 / 更好的换弹体验 / 更好的尸潮播报。
- LIR 未来新增子能力不新建频道身份，除非它实际成为独立功能模块；各功能未来的新独立能力按扩展三分法立新 FeatureId。

### LIT：整理策略 seam 与算法（T4 决策 1）

- 排列算法原样迁移（行为不变），但立即立策略 seam，结构：背包整理功能 → `ITidyStrategy` → 当前策略（`default-grid-v1` adapter）→ InventorySolver。接口最小形（来自决策票）：`string StrategyId { get; } TidyPlan BuildPlan(TidyInput input);`，输入输出为纯 C# 类型（InventorySolver 本就零 Unity 依赖）。
- **O-LIT-1 勘误口径**：用户诉求是排序规则（进入计划的先后与分组），不是放置算法（占位 / 旋转 / 坐标）；两者都是策略内部维度，本阶段两者都不改。
- 不做：策略选择器 UI、第三方 DLL 动态加载；`StrategyId` 进设置 = 后续「设置与策略治理」票，不阻塞纳入。

### LIT：设置、熔断与夹具（T4 决策 3/4/5）

- 本期只持久化 `enabled`（ClientLocal，SettingsRuntime 唯一权威；关闭 → 原生整理回退）；每页方向 / 模式保留内存态并标记为非持久化临时状态，未来持久化时不得把旧静态字典直接升格为设置事实源。
- 熔断作用域改绑 BUE 连接代际：连接建立 / 新代际 → 开始本次会话 fault scope；断线 / 代际更替 → 关闭旧 scope + 清内存态 + **保留磁盘持久化统计**（功能私有 JSON 键结构不变，不复制进第二事实源）。旧 `SteamP2PFriends.BeginScope("p2p")` 调用方随迁入消失，由会话代际取代（BUE 仓库无该类型引用的老问题就此消掉）。
- TIDY_TEST_HARNESS 及全部实机夹具类型归档不迁入，**硬规则排除出生产编译列表**；红测面按新 seam 重写（见 Testing Decisions）。页范围维持玩家页 2–6；UI 按钮 Harmony postfix 留在模块内。

### LIT ↔ LIR：TidyCompleted 功能事件（T4 决策 6 + T5 决策 3）

- 链路：LIT 发布 `TidyCompleted` 功能事件（宿主事件总线）→ LIR 消费，验结果为成功且范围符合 → 经 `TidyCompletedConsumer → ReloadAction`（本期 adapter = 整理后自动压弹）执行。消除按类型名反射寻找 LIT、跨功能 Harmony postfix、异常静默吞。
- **事件类型落位（T5 具名移交，本规格定稿）：`TidyCompleted` 事件类型入 Contracts 冻结面**——功能事件是跨功能协作的唯一公开缝且官方 / 生态同权（CONTEXT「功能事件」），生态消费同一事件与 LIR 同权；随本次契约版本升级一并登记。事件类型为只读 struct，载荷至少含：发布者 FeatureId、整理对象范围、完成结果、连接代际（如适用）、事务 / 操作标识；事件身份字符串由发布者 FeatureId 派生，具体命名在实施票按契约登记流程定稿。
- LIR 不得见事件就盲执行：必须验成功 + 范围，保持自身幂等与异常隔离；本期不加「整理后自动压弹」开关（enabled 总开关已存在）。

### LIR：补丁、守卫与常量（T5 决策 2/5）

- 三枚补丁保留为模块内部实现：`UseableGun.ReceiveAttachMagazine` Prefix/Postfix、`PlayerInventory.forceAddItem` Prefix；结构：LIR 生命周期 → ReloadContextGuard → 三个 Harmony adapter → 换弹行为。
- `Stop` 只撤销 LIR 自己的 Harmony ID。冲突审查事实（本票已核）：BUE / BII 现有补丁目标与 LIR 三补丁零交集。
- 常量集中到内部策略对象 `ReloadRuntimePolicy`（双击窗口 0.3s、队列上限 64、诊断间隔 5s），不散落补丁；本期非公开设置。
- 扩展三分法：换弹策略扩展走 LIR 内部 `IReloadAction` adapter；真正独立的新能力（自带 FeatureId / 设置 / 生命周期 / 频道）立新官方功能模块；跨功能协作只走公开契约（TidyCompleted、HostTick），不恢复直接调用 / 反射寻类型 / Harmony postfix。

### LHT：双件结构与传输上收（T6 决策 2/3/5/6）

- 自有补丁 = 信标两枚 Postfix（**勘误**：答案曾误列 LIR 的补丁，已按信标补丁入账），Harmony ID 收编 LHT FeatureId；上下文守卫原则冻结：补丁可共享原生调用点，不能共享业务上下文——LHT adapter 只捕获尸潮追踪上下文，非相关路径立即放行，不把 BII 状态当事实源。
- 内部双件（一个 FeatureId 内部组合，非两个注册功能）：`HordeTrackingModule`（信标追踪 / epoch / seq / 广播，服务器权威）+ `HordePresentationAdapter`（HUD 注入 / 10Hz 更新）；HUD 失败只降表现不伤权威追踪。主机 / U3DS 权威追踪 + 广播快照、不给本地主机 loopback（会话驱动组播天然保证）；客户端收快照 + HUD；U3DS 功能状态 Available、表现状态 HeadlessOnly（不阻塞 U3DS 功能验收）。
- 删除：`ModTransport.Initialize` 直调、LMN ABI 守卫、对 LMN 类型与传输实现的硬依赖。保留业务一致性实现：epoch、sequence、单槽 mailbox、脏标记、Clear 可靠发送、Update 不可靠发送、停止闸门。
- 广播 = `SendToClients` 会话驱动组播整体替换 `Provider.clients` 手写循环 + 跳过本地；可靠度不变（Update 不可靠、Clear 可靠）；行为基线 = 08 实机（epoch 完整生命周期、Update seq 连续无重复键、不可靠通道 1:1）。
- 设置：只持久化 `enabled`（ClientLocal）；关闭 = 完整停摆（服务器停追踪停广播、客户端停 HUD、`/horde` 无 LHT 行为）；`/horde` 冷却 1.5s、HUD 10Hz 为实现常量；admin 权限判断继续交给原版 ChatManager 守门，LHT 不复制权限事实源。
- 扩展三分法：追踪策略走 `IHordeTrackingPolicy`（默认实现承载现行为）；表现变体走 `HordePresentationAdapter` 下扩展（原版 HUD / 聊天播报 / 未来面板）；独立新功能立新 FeatureId 模块。

### 宿主时钟（T5 决策 4 + T6 决策 7）

- 契约形态（来自决策票）：`struct HostTick { ulong TickNumber { get; } float DeltaTime { get; } TickPhase Phase { get; } }`。
- **落位定稿：入 Contracts 冻结面**（同 TidyCompleted 理由；T6 明示未来生态功能订阅同一时钟 seam，不允许「官方能用、生态拿不到时钟」）。
- 冻结不变性（两票合并）：时钟由宿主统一产生；频率与阶段明确固定；回调主线程执行；序号单调、携带时间增量；载荷只含时间与序号、不携带功能逻辑；功能停止自动注销；单订阅者异常不扩散；功能模块不得各自创建 Unity Update 泵。
- LIR 订阅后自负责双击检测、换弹键轮询、换弹状态推进、超时判断；LHT HUD 以 10Hz 消费同一 seam。

### 开发者契约与 SDK 引用（T7 决策 1/2）

- 三段式契约：**承诺** = 插件 GUID `io.github.yu80rice.betterunturnedexperience` 冻结；程序集名 `BetterUnturnedExperience` 冻结（未来若要改，不承诺兼容，必须作为破坏性公告和迁移事件处理）；公开契约按契约版本演化，冻结面破坏性变更必升版本并登记。**不承诺** = 非 BepInEx 加载方式、未验证的 Preloader `AssemblyResolve` 内部行为、任意改程序集名后仍兼容、任意重命名 / 复制 / 阴影加载后的行为、把单次加载实验当永久 ABI 保证。**编译期指引** = 引用官方主 DLL、`CopyLocal=false`、禁止把 BUE DLL 捆进第三方发布包、契约版本与目标 BUE 版本对齐；核心 = 第三方只引用公开 interface，不复制 implementation。
- 文件名精确措辞：受支持的 BepInEx 加载方式下，DLL 文件名不是 BUE 的稳定契约身份，第三方绑定依赖插件 GUID 与程序集身份；文件路径、加载目录与 BepInEx 发现规则属部署前提。用户期待已登记：未来版本更新可变的是文件名，程序集名不动。
- SDK 引用面 = 直接引用主 DLL + `CopyLocal=false`；不拆独立 SDK 程序集（出现四条件之一才立项：第三方需脱离完整 BUE DLL 编译 / 多仓库需稳定纯契约包 / 发布节奏须独立 / 需公开桥接 adapter 而不暴露主程序集）。
- 依据（T1 实证）：前置解析按插件 GUID（拓扑 + 硬依赖检查）、运行时类型绑定按程序集身份、Awake 序 = GUID 拓扑序——均与文件名无关，Forge-like 承诺可行。

### 防双装（T7 决策 3）

- 机制：BUE 启动（Awake）注入式自检——取已进入 AppDomain 的程序集列表，检查与 BUE 同程序集名的冲突副本，输出结构化诊断，诊断 id = `BUE-PLATFORM-001`；字段至少含：检测到的程序集名、冲突副本 Location、当前 BUE 路径、「移除非官方副本」建议。**不自动删除用户文件**，处置留给用户；诊断集中平台模块。
- 边界（措辞冻结）：只处理已进入 AppDomain 的程序集；不替代 BepInEx 的 GUID 去重；不保证捕获未加载 / 加载失败 / 隔离上下文中的副本；**不宣传为完整防重复加载系统**。
- 分工：同 GUID 双装 = BepInEx 原生行为（留一跳一、单实例、不报 fatal）+ 文档 FAQ；同程序集名不同 GUID = BUE 自检补强。

### 开发者文档（T7 决策 4/5）

- 扩写现有 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`，不新建第二文档目录（避免双事实源）。大纲八节冻结：1 适用范围 / 2 承诺 / 3 不承诺 / 4 编译期引用指引 / 5 GUID·程序集名·DLL 文件名 FAQ / 6 双装诊断 BUE-PLATFORM-001 / 7 契约版本演化 / 8 实机验证清单；正文随实施票完成。

## Testing Decisions

### 测试原则

- 只测外部行为，不测实现细节；红测先行（编译红或运行时红 → 实现转绿），沿用仓库 `--<ticket>-red` 锚点惯例；实施期每个产出物过双轴独立审查（standards-reviewer / Spec-Reviewer 专属智能体，不用 general-purpose）CLEAN 才交付。
- 网络 / 契约面全部纯 C# seam 测试（Contracts 不引用引擎类型）；真实收发、三环境行为走实机验收，绑定 LoadSetIdentity。

### Seam 总图（尽量用既有缝；新缝均已拍板）

1. **`IBueNetworkApi` / `IFeatureBootstrap.Network`**（既有缝升级）：三插件与生态功能的全部网络行为（注册、订阅、方向、发送结果、会话快照、停用语义）经假 adapter 注入测试；运行时侧按方向双 handler 表、组播、握手以行为断言驱动。
2. **宿主注册与生命周期面**（既有缝）：三功能经 `IFeatureRegistration` 注册，`IFeatureModule.Start/Stop` 驱动安装 / 撤销、隔离、完整停摆、静态表代际绑定。
3. **帧分类与接管决策核**（既有缝，扩展 BUE 帧分支）：测试直驱决策核、从不安装 Harmony 补丁（沿用现有「生产 prefix 是 `ShouldConsumeInbound` 唯一调用者」纪律）；BUE 帧 / MOD / LMN2 / vanilla 四类分类与六步决策顺序在此钉死。
4. **`ITidyStrategy`**（新缝，T4 拍板）：策略替换、StrategyId、纯类型计划输出；InventorySolver 纯算法直测保留。
5. **`ReloadContextGuard` / `IReloadAction`**（新缝，T5 拍板）：叠加点行为以 guard 输入输出钉死，不装补丁。
6. **宿主时钟注入**（新缝，T5/T6 拍板）：假时钟驱动 LIR 双击 / LHT HUD 节奏；不变性（单调、阶段、异常隔离、停止注销）在此测。
7. **程序集列表注入**（新缝，T7 拍板）：防双装自检注入程序集清单、不触文件系统。
8. **功能事件总线**（既有缝）：TidyCompleted 发布 / 订阅 / 校验语义经 `IOwnedFeatureEventPublisher` / `IFeatureEventSubscriber` 测试。

### 逐票红测面

- **T3**：订阅方向语义与独立句柄；fail-fast 参数与「订阅未注册频道合法」；停用 = 可订阅 + 入站为零 + `NoSession`；发送结果五值含 `PartialFailure`；会话快照 established-only；握手（Ack 按 peer+代际匹配、重复 Hello 去重、Reject fail-closed、Connected 仅握手后）；决策核六步顺序与「两 seam 不共用布尔」；魔数 BUE1 帧编解码回归。
- **T4**：策略替换测试；`enabled=false` 原生回退；连接代际切换下 fault scope（清内存、留磁盘）；InventorySolver 直测；TidyCompleted 事件载荷与发布语义；夹具类型不在生产编译列表（编译期断言）。
- **T5**：叠加点红测——BII 拖入 → `forceAddItem` Prefix 触发 → 上下文 = false → LIR 不执行换弹；`Stop` 只撤自身 Harmony ID；TidyCompleted 消费验成功 + 范围 + 幂等；HostTick 六条不变性。
- **T6**：信标 Postfix 上下文守卫（context = false → 立即放行）；会话驱动组播不含本地身份；**BUE 帧不可靠 1:1 复验**（T2 移交点，08 基线是 LMN 路径，须在 BUE 帧路径重证）；enabled=false 完整停摆；U3DS Available + HeadlessOnly。
- **T7**：注入面红测六例——无冲突 / 同程序集名冲突 / 不同程序集名 / 空路径 / 重复条目 / 诊断 id 与关键字段。

### 实机验收面（目的地四条 + T7 清单）

- 裸 BUE 单 DLL（无 LMN、无三插件 DLL）：四官方功能在单人 / SteamP2PFriends / U3DS 全部可用；面板显示四件官方中文名；三插件即 BueNetworkApi 生产绑定第一批真实消费者；未知 V1 旧插件在「BUE + 独立 LMN」部署下共存承诺不破坏（裸 BUE 无独立 LMN 除外）。08 kit（接管态 + 独立 LMN）为对照基线，不替代终态验证。
- T7 实机五项清单（入实施票）：改名实机对照；Mono `LoadFile` 二次探测；同版本程序集最终谁保留；不同 GUID + 同程序集名（**红测 + 实机双证**，红测不能替代实机结论）；Preloader `AssemblyResolve`。

### Prior art

- 七个既有测试工程（Contracts / Network / Plugin / Placement / Settings / ClientUi / Release .Tests）；`DiagnosticLogSink` / `BueRuntimeLog.Recorder` seam 与 `--*-red` 锚点；契约断言走 Contracts.Tests；接管决策核直驱测试模式（NetworkModuleAdapter 纪律）。
- 证据门禁沿用三环境证据包 + LoadSetIdentity 流程；RELEASES 加行随实施票候选节奏。

## Out of Scope

- **整理排序规则变体**（compact / 按类别分组 / 最少移动等 `ITidyStrategy` 变体、`StrategyId` 进设置）：seam 已预留，归后续「设置与策略治理」票。
- **DEV-V2-12 N-1**（LMN 内部出站首 ping 竞态）：保持挂起；新会话寻径下出站目标必然已握手，同类竞态面在 BUE 新路径不存在。
- **DEV-V2-13 具名设计取舍 2–5**：已有意不动。
- **SDK 分发与版本策略细化**（第三方从哪获取引用 DLL、示例插件形态）：挂在 T7 结论上，出现 T7 决策 2 四条件之一或生态需要时立票。
- **玩家迁移指引**（旧「BUE+LMN+三插件」部署 → 只装 BUE 的迁移说明、LMN DLL 保留语义）与**原 Archive 三仓库退役公告**：随纳入实施与发布节奏另行产出。
- **三环境验收票、RELEASES 加行、真机手册的具体编排**：随 `/to-tickets` 产生。
- **其他能力迁移**（本地联机、性能优化、背包整理以外的 Launch 能力）：后续阶段，目的地重绘时立新 effort。
- **git 远程备份**：基础设施 chore，待用户提供 URL。

## Further Notes

- **难度与顺序**：迁移难度 LHT < LIR < LIT（T2 结论）；T3 契约落地顺序按「订阅 → Network 注入 → 发送 → 握手 → 帧/接管拆分」；三功能纳入的工单排序交 `/to-tickets`。
- **勘误已入账**：O-LIT-1 要改的是排序规则而非放置算法（T4）；LHT 自有补丁是信标两 Postfix 而非 LIR 补丁（T6）；「BepInEx 按文件名序加载」系讹传，实为 GUID 拓扑序（T1 二次勘误）。
- **词汇纪律**：产品语言用数字频道 / 命名频道，V1/V2 是内部词汇；频道身份是 FeatureId 不是 GUID；帧语言一律「BUE 帧」。
- **用户期待登记**：未来版本更新可变文件名，程序集名不动——动了前置引用就断（契约两锚推论）。
- **交付入口**：纳入完成后 BUE 是玩家侧唯一推荐交付入口；旧项目可保留历史页面与迁移说明，不再要求玩家下载分散 DLL（CONTEXT「官方纳入后的交付入口」）；仓库对吸收的实现保留来源署名（CONTEXT「实现来源署名」）。
