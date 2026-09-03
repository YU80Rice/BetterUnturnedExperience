# V1 数字频道生态插件盘点（V2-T8 research）

Wayfinder 票：`V2-T8-v1-ecosystem-inventory`（open）
目的：为 `V2-T4` Q4 的「已知生态插件迁移比例阈值」提供数据基线。
方法：只读调查 LMN 源码 + 本机 mod 工程 / 存档 / 已装插件 / 启动器 + 网络检索。**未改任何源码，未关闭工单。**
调查日期：2026-09 会话。

---

## 结论

### 1. 「什么算用 V1」——可判据（从 LMN 源码固化）

一个 BepInEx 插件如果是 **LMN 的 V1 数字频道消费方**，当且仅当它对 **LMN 的 V1 数字频道公开 API 调用面** 发起了调用。这一调用面在 `LaunchMultiplayerNet` v5.0.0 中如下（都是 `int virtualChannel` 形态）：

**判据 A —— 注册/注销（V1 处理器表 key 为 `int` 频道号）：**
- `ModTransport.RegisterServerHandler(int virtualChannel, Action<CSteamID, BinaryReader>)` — `Routing/ModTransport.cs:137`
- `ModTransport.RegisterClientHandler(int virtualChannel, Action<BinaryReader>)` — `Routing/ModTransport.cs:159`
- `ModTransport.UnregisterServerHandler(int, ...)` / `UnregisterClientHandler(int, ...)` — `Routing/ModTransport.cs:186,216`
- 兼容别名 `ModP2PTransport.*`（`[Obsolete]` 但保留，全部委托到 ModTransport）— `Routing/ModP2PTransport.cs:16-31`
- 抽象表面 `IModTransport.RegisterServerHandler(int, ...)` 等 — `Routing/IModTransport.cs:27,30,44,47,50`

**判据 B —— 发送（int 频道隐式传入 BuildModPacket）：**
- `ModTransport.SendToServer(int virtualChannel, byte[], bool)` — `ModTransport.cs:415`
- `ModTransport.SendToClient(CSteamID, int virtualChannel, byte[], bool)` — `ModTransport.cs:456`
- `ModTransport.BroadcastToAllClients(int virtualChannel, byte[], bool)` — `ModTransport.cs:495`
- `ModTransport.BuildMessage(EModMessage, ...)`（用全局 V1 子消息枚举）— `ModTransport.cs:818`

**判据 C —— 线格式身份（运行时证据）：** 任何进/出 `MOD` 魔数帧且首字节为 0..255 频道号的流量。`ModRouter` 将 `BuildModPacket` 打成 `["MOD" 3x][virtualChannel:1byte][payload]`，接收到 `MOD` 帧则按首字节频道号路由到 `ServerHandlers`/`ClientHandlers`（`Routing/ModRouter.cs:13-16,40-51,73-107`）。V2 用独立 `LMN2` 魔数（`Routing/ModRouter.cs:18-24`），互不解析（`CHANGELOG.md:25`、`NAMED_CHANNEL_STANDARD.md:208`）。

> 注意：**仅声明 `[BepInDependency]`（硬/软）但不调上述 API 不算 V1 消费方**——它没占用任何频道、不参与帧路由。区分"编译期引用 LMN"与"运行时使用 V1 频道"是本盘点最关键的判定线。

### 2. 已知插件清单与迁移状态

LMN 生态**全部是本机作者 YU80Rice 的模组家族**（见「局限」：无外部可证下游）。按"是否实际调用 V1 API"分类：

#### 2a. 历史上被官方分配 V1 频道的发布消费方（LMN 作者维护）—— **均已迁 V2**

| 插件 | 历史 V1 频道 | 来源（git remote） | 现源码 API | 迁移状态 |
|---|---|---|---|---|
| **LaunchInventoryTidy (LIT)** | 100 `TidyPage` | `github.com/YU80Rice/LaunchInventoryTidy.git` | `RegisterNamed*` / `SendNamed*`（`ManualTidyNetwork.cs`） | **已迁 V2**；发布 v3.0.1 |
| **LaunchInPlaceReload (LIR, AmmoRepacker)** | 101 `RepackAmmo` | `github.com/YU80Rice/LaunchInPlaceReload.git` | `RegisterNamed*` / `SendNamed*`（`AmmoRepackNetwork.cs`） | **已迁 V2**（git `a6b5964 migrate LIR to LMN V5`）；发布 v3.0.0 |
| **LaunchHordeTracker (LHT)** | 102 `HordeStatus` | `github.com/YU80Rice/LaunchHordeTracker.git` | `RegisterNamed*` / `SendNamed*`（`HordeStatusNetwork.cs`） | **已迁 V2**；发布 v3.0.0 |

频道 100/101/102 由 `Routing/ModChannels.cs:10,13,16` 固化为历史登记；V1 → V2 迁移发生在 V5 重构期（`CHANGELOG.md` v4 "已重新编译" 名单 + git 迁移提交）。LMN 现源码**仍保留** V1 通道只为兼容这些历史消费方。

#### 2b. 仍在源码中实际使用 V1 数字频道的消费方（**未发布/已存档**）

| 插件 | 频道 | 证据 | 状态 |
|---|---|---|---|
| **LaunchSecureContainer** | **103** | `SecureContainerNetwork.cs:26` 定义 `VIRTUAL_CHANNEL = 103`；`:38-39 RegisterServerHandler(103) / RegisterClientHandler(103)`；`:56,73,91 SendToServer(103)`；`:129 SendToClient(target, 103)` | **仍用 V1**。无 git、无部署 DLL、位于 `Archive\2-未闭环验证项目`（未闭环），**从未发布** |

注意：LMN 的 `ModChannels.cs` **没有**登记 103。LaunchSecureContainer 是自行申请/猜测的频道，也未申请新频道（违反 V5 后"新消费方不得申请 V1"规则，但因其未发布未构成已发布生态约束）。

#### 2c. 引用 LMN 但**未实际调用** V1/V2 频道 API（仅依赖声明或框架）—— 不算 V1 消费方

| 插件 | 证据 | 备注 |
|---|---|---|
| **LaunchAcidBalance** | `LaunchAcidBalancePlugin.cs:11` 仅 `[BepInDependency("com.umm.launchmultiplayernet", SoftDependency)]` + csproj 引用；**零 `ModTransport.*` 调用** | 在 `08-consumer-plugins-migration.md:19` 列为 V5 重编译通过，指编译期引用 |
| **LaunchEntityCuller** | 全树 grep 无 `ModTransport`/`ModChannels` 调用 | `08` 票同样列为重编译通过（编译期引用） |
| **LaunchFlowFieldNav** | `Plugin.cs:26` 设计为「LMN v4.0.0 虚拟通道 103」，`Plugin.cs:37` 硬依赖；但 `Net/FlowFieldNetSync.cs` 文件**不存在**（工程残缺） | 未完成、通道 103 与 SecureContainer 冲突、未发布 |
| **LaunchItemBrowser / LaunchPerfOptimizer / LaunchWorkshopAccelerator / WaterPerfOptimizer** | 全树 grep 无 LMN 引用 | 不属 LMN 生态 |

#### 2d. 已安装 / 部署状态（本机实况 `E:\Steam\...\BepInEx\plugins\`）

- 仅 `BetterUnturnedExperience.dll`（BUE 自身）与 `SteamP2PFriends.dll.disabled`。
- BUE 源码（`更好的UN体验\src`）grep 无 `LaunchMultiplayerNet`/`ModTransport` — **BUE 尚未接入 LMN**（符合 V2 阶段定位）。
- `SteamP2PFriends`（两个目录）grep 无 LMN 引用。
- 本机**没有任何已部署的旧 V1 插件**在调用 LMN V1。—— 但注意 LIT 目录内存在 `LaunchInventoryTidy.dll`（v3.x）本体，见「局限」。

### 3. 迁移比例基线（供 V2-T4 Q4 阈值计算，基于已知样本）

给定上文判据和已知样本，可得两个口径的基线。**说明：这是已知样本口径，不是穷举口径**（见「局限」）。

- **口径甲（官方已发布 + 持有 V1 频道的消费方）**：LIT、LIR、LHT 共 3 个，**3/3 已全部迁移 V2** → 迁移比例 **100%**。这是 LMN README/CHANGELOG 官方点名的全部已发布 V1 频道持有者。
- **口径乙（本机所有曾实际使用 V1 的消费方：3 发布 + SecureContainer）**：共 4 个，已迁 V2 3 个，仍 V1 1 个（SecureContainer，未发布）→ 迁移比例 **75%**；**仍处 V1 的 1/4 = 25%**。
- **口径丙（所有硬依赖 LMN 的消费方，含凑数工程）**：LIT/LIR/LHT/FlowFieldNav(+ 编译期仅 SoftDep 的 AcidBalance/EntityCuller) … 硬依赖者已全迁或未完成，V1 活跃使用者只有未发布的 SecureContainer。

**建议给 T4 的判据锚点**：迁移阈值应基于「口径甲」——LMN 公开发布的 V1 频道持有者已 **100%** 迁移 V2；唯一仍在源码用 V1 的 `LaunchSecureContainer` 从未发布、无部署、不构成已发布生态负担。因此 BUE 的 V1 兼容层服务对象在已知数据内**趋近于零个仍活跃的已发布插件**，若把"已发布生态插件迁移比例 ≥ 某阈值"作为退出条件，样本上看该条件已达成（100%）；真正长期需要 V1 兼容的，只剩"未知第三方/未发现的老旧已装插件"——本盘点在本机未发现已部署实例。

---

## 证据（文件 + 行号）

**LMN V1 API 判据源码**（`D:\...\LaunchMultiplayerNet\`）：
- `Routing/ModTransport.cs:137,159,186,216` V1 Register/Unregister 处理器（int 频道）
- `Routing/ModTransport.cs:415,456,495` V1 SendToServer/SendToClient/BroadcastToAllClients（int 频道）
- `Routing/ModTransport.cs:818` V1 `BuildMessage(EModMessage,...)`
- `Routing/ModTransport.cs:695-704` `ValidateLegacyChannel`（0..255 范围校验，V1 频道判据）
- `Routing/ModRouter.cs:13-16` `MOD` 魔数；`:40-51` `BuildModPacket(int virtualChannel, byte[])`；`:73-107` `TryHandleLegacy*` 按首字节频道号路由
- `Routing/ModP2PTransport.cs:16-31` `[Obsolete]` V1 兼容别名
- `Routing/IModTransport.cs:27,30,44,47,50` V1 抽象面
- `Routing/ModChannels.cs:10,13,16` V1 频道登记：100/101/102（LIT/LIR/LHT）
- `Routing/NamespacedTransport.cs` — V2 对照：`RegisterServerHandler(string pluginGuid,...)` 等
- `Core/LaunchMultiplayerNetPlugin.cs:18`「V1 保留 + V2 使用」设计说明；`:44` GUID `com.yu80rice.launchmultiplayernet`
- `NAMED_CHANNEL_STANDARD.md:13,200-208` V1 仅历史兼容、V1/V2 魔数互不抢占
- `README.md:67` 历史插件可继续调 V1 API；`:71` 命名频道已由 LIT/LIR/LHT 使用
- `CHANGELOG.md:24` V1 保留；`:43` v4 重编译消费方名单；`:116-120` 已验证消费方（LIT 100/LIR 101/LHT 102）；`:263-265` 频道分配史

**消费方 API 使用（`D:\...\Archive\2-未闭环验证项目\`）**：
- `LaunchInventoryTidy\ManualTidyNetwork.cs:152-153,473,1100,1479,1891,1925` — `RegisterNamed*` / `SendNamed*`（V2）
- `LaunchInPlaceReload\AmmoRepackNetwork.cs:381-382,456,614` — `RegisterNamed*` / `SendNamed*`（V2）；git 迁移提交 `a6b5964`
- `LaunchHordeTracker\HordeStatusNetwork.cs:70,323` — `RegisterNamed*` / `SendNamed*`（V2）
- `LaunchSecureContainer\SecureContainerNetwork.cs:26` `VIRTUAL_CHANNEL=103`；`:38-39,56,73,91,129` — `RegisterServerHandler/ClientHandler/SendToServer/SendToClient`（V1）
- `LaunchAcidBalance\LaunchAcidBalancePlugin.cs:11` — 仅 SoftDependency
- `LaunchFlowFieldNav\Plugin.cs:26,37` — 设计通道 103 + 硬依赖，但 `Net/FlowFieldNetSync.cs` 缺失
- `LaunchMultiplayerNet\.scratch\v5-rebuild\issues\08-consumer-plugins-migration.md:14-19` — 5 款下游重编译通过（LIT/LIR/LHT/LaunchEntityCuller/LaunchAcidBalance）

**发行与分发点**：
- `LaunchMultiplayerNet\TECHNICAL_HANDOFF.md:8` — GitHub Release `github.com/YU80Rice/LaunchMultiplayerNet/releases/tag/v5.0.0`
- LMN + 三个发布消费方 git remote 均 `github.com/YU80Rice/*`（本机 `git remote -v` 实测）
- `启动器\UnturnedModManager\.qa\...\community\*.json` — 社区目录缓存仅见 `PluginManagerMod`，未列出 Launch*/LMN 生态插件

---

## 迁移状态未知项

以下**无法**由本调查确定，留待更广来源或后续核实：

1. **GitHub 上的外部下游 / stars / forks / release 下载量**：网络检索未找到 LMN 或其消费方之外的任何使用证据；浏览器（bsk）无连接浏览器、无法爬取 GitHub 页面去确认这些仓库是否存在非本机外部用户、是否有 fork/issue/discussion 提及第三方接入。结论「全为作者家族」主要基于本地证据，外部无证和无证皆可（无法排除存有未建档的外部使用者）。
2. **创意工坊（workshop）实操位**：本机 `E:\Steam\steamapps\workshop\content\304930\` 44 个目录均**未检出** `LaunchMultiplayerNet`/`ModTransport`/`BepIn` 引用（BepIn 插件通常不通过 Unturned 创意工坊分发，多为地图/服 Mod），但不能断言线上无任何分发。
3. **未归档的历史/已装机插件**：`Archive\1-已退役探索项目`（LaunchP2PDiagnostics 等）未逐一深查其 LMN 调用面（初步 grep 集中于 `2-未闭环验证项目`）；`4-历史发布与备份` 与 `Libs\` 未逐文件比对是否混有旧版 V1 DLL（LIT 目录内存在 `backup-LaunchInventoryTidy.dll` 与 `LaunchInventoryTidy.dll`，未验证其内嵌 API 面）。
4. **每个已迁 V2 插件的「旧版已发布 V1 二进制」是否仍被装机使用**：源码是 V2，但 LIT/LIR/LHT 的旧 v1.x-v2.x 二进制曾发布；本机已装 `BepInEx\plugins` 只有 BUE+SteamP2P，无法证明玩家群里跑什么版本。
5. **`LaunchSecureContainer` / `LaunchFlowFieldNav` 是否曾外发**：它们无 git、无部署 DLL、在"未闭环"目录，判定为未发布，但无法 100% 排除手工外传。

---

## 局限（采样边界）

- **非穷举**。LMN 生态在本机就是 YU80Rice 单作者 mod 家族全集；"导出到公共生态"只反映为 GitHub 单一组织（YU80Rice）下的仓库。**外部/线下/其它服务器的未知第三方消费方无法从本机数据枚举**——本盘点只能给出"已知样本 + 可判据"，阈值应被理解为**基于已知样本的下限基线**，且应在 V2-T4 验收（`08` 票 Q8：干净环境装旧 V1 no-op fixture 可收发）之外，另行把"未知第三方"作为长期兼容保留理由。
- 语言/平台采样：网络检索（web_search）对 `LaunchMultiplayerNet`/`YU80Rice` 返回无关结果（水稻数据库等噪声），无有效外部信号可引用；这是"无证"而非"证无"。
- 本调查只读源码/文本 + web，未做 IL/二进制反编译来 100% 确认每个 DLL 内嵌 API 引用；对 `LaunchSecureContainer`/`LaunchFlowFieldNav` 的"未发布"判断基于目录语义 + 无 git + 无部署 DLL，非绝对反证。
- 判定"已迁 V2"仅依据**当前归档源码**与 git 迁移提交，未复跑 V5 编译验证（重编译证据引用 LMN `08` 票的既有结论，本调查未重新构建）。

---

## 给 parent / 后续（V2-T4 联动）

- **判据可执行**：任何"是否用 V1"判定 = 检查插件对 LMN 是否调用 `Register*Handler(int,...)`/`SendToServer|SendToClient|BroadcastToAllClients(int,...)`/`BuildMessage(EModMessage,...)`（或 via `MOD` 魔数帧）。
- **已知样本基线**：已发布 V1 频道持有者 3/3 = 100% 迁 V2；唯一活跃 V1 使用者（SecureContainer 103）未发布。→ T4 退出阈值在"已发布已知生态"口径上可由 定义数据满足；长期 V1 兼容的理由转为主要由"未知第三方/未发现旧装"承担。
- 建议把"覆盖外部/创意工坊/线下装机"作为 T4 阈值公式的显式未知项，避免把 100% 本地样本误读为生态全迁。