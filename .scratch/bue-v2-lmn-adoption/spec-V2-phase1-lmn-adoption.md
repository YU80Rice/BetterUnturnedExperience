# V2 第一阶段规格：LMN 官方纳入与 BueNetworkApi

Type: spec
Status: ready-for-agent
Parent: V2 开放运行时平台方向（CONTEXT.md 已冻结的 BUE V2 产品方向）
Source: wayfinder 地图 `.scratch/bue-v2-lmn-adoption/map.md`（7/7 票 + 4 份 research 全部 resolved，2026-09-03）
Author: GPT（/to-spec 合成；决策经人工 grilling 拍板）

---

## Problem Statement

BUE 目前是"只能把功能源码聚合进一个 DLL"的插件产品。Unturned 官方与 BepInEx 官方都没有提供 Forge 式的客户端间第三方通信平台（CONTEXT「客户端间第三方通信平台」），导致 BUE 的 U3DS 与本地联机环境缺少插件交流通道。LMN（LaunchMultiplayerNet）已实现该能力（V2 命名频道 + V1 数字频道），但作为独立插件存在：玩家需分别下载独立 LMN DLL 才能获得网络功能；且 BUE 与独立 LMN 同装时存在双网络实现竞争风险。

玩家侧应当只部署一个 `BetterUnturnedExperience.dll`；官方网络能力应内置、可关、故障隔离、与第三方功能平权。

## Solution

BUE"吃掉并消化"LMN：V2 第一阶段把 LMN 纳入为 BUE 官方网络模块，内置 `BueNetworkApi`（V2 命名频道 + 版本协商），保留 V1 数字频道兼容路径（旧插件无改动运行），提供 BUE/独立 LMN 共存接管（检测 + 优先权抢占停用 + 面板可逆），配置迁移为空操作（LMN 无配置系统），并在单人 / U3DS / SteamP2PFriends 三环境完成网络层验证。玩家继续只部署 `BetterUnturnedExperience.dll`。

## User Stories

1. 作为玩家，我希望启动游戏后 BUE 内置网络能力直接可用，以便与好友本地联机时插件功能互通，无需额外下载独立 LMN DLL。
2. 作为玩家，我希望 BUE 管理面板能看到"已由 BUE 接管"的网络状态，以便确认独立 LMN 已被接管而非双跑。
3. 作为玩家，我希望网络模块可以在面板中关闭，以便不需要联机功能时保持本地轻量。
4. 作为玩家，我希望关闭 BUE 网络模块后独立 LMN 恢复原样运行，以便可逆地切换回旧方案。
5. 作为玩家，我希望网络模块故障不影响 BII 等本地功能继续运行，以便一个功能出问题不拖垮整个游戏体验。
6. 作为旧插件用户（依赖 LMN V1 数字频道），我希望旧插件无改动继续收发，以便升级 BUE 不破坏既有 mod 生态。
7. 作为旧插件用户，我希望面板显示"无独立配置可迁移（LMN 无配置文件）"，以便不误以为有配置丢失。
8. 作为第三方功能开发者，我希望通过 `BueNetworkApi` 注册一个命名频道（FeatureId 即频道名），以便与对端同类功能通信。
9. 作为第三方功能开发者，我希望频道版本协商由 BUE 自动完成（不匹配返回 `ContractIncompatible`），以便无需自行实现握手。
10. 作为第三方功能开发者，我希望发送/接收 API 屏蔽底层链路差异（客户端→服务器走 `IClientTransport`、服务器→客户端走 `ITransportConnection`），以便不感知方向不对称。
11. 作为第三方功能开发者，我希望通过会话事件订阅（Connected/Disconnected/GenerationChanged）感知连接生命周期，以便及时降级。
12. 作为第三方功能开发者，我希望发送失败返回稳定枚举（`NetworkSendResult`）而非异常，以便热路径无异常开销且可本地化。
13. 作为第三方功能开发者，我希望 `BueNetwork` 命名空间的公开契约不引用 Unity/Glazier/Sleek/LMN/Unturned/BepInEx 具体类型，以便按规范独立实现功能。
14. 作为第三方功能开发者，我希望官方 Better Item Interaction 与第三方功能走同一注册/生命周期路径，以便不维护官方/第三方分支。
15. 作为服务器管理员，我希望 U3DS Headless 不实例化任何 UI/ClientUi 类型，以便无头服务器安全运行。
16. 作为服务器管理员，我希望网络层故障只隔离网络功能、不触发全局核心安全降级，以便原版游戏继续运行。
17. 作为发布维护者，我希望三环境证据绑定完全相同的 LoadSetIdentity，以便不被单一 DLL 哈希冒充整体验证。
18. 作为旧插件作者（LIT/LIR/LHT），我希望功能并入 BUE 官方发行版而非独立维护，以便玩家侧收敛为单 DLL。

## Implementation Decisions

### 决策来源

全部来自 wayfinder grilling（人工拍板）+ research 查证；研究证据见 `.scratch/bue-v2-lmn-adoption/research/`（T1/T5/T6/T8 四份报告）。契约类型写入 `ContractTypes.cs` 与 BueNetworkApi 实现均须另立 DEV 工单，按 output-review-loop（红测先行 → 双轴审查 → 提交）实施。

### 物理部署与依赖身份

- 玩家侧始终单 DLL `BetterUnturnedExperience.dll`；仓库内部允许模块化开发、构建时聚合。
- BUE 主插件 GUID `io.github.yu80rice.betterunturnedexperience`；LMN 独立插件 GUID `com.yu80rice.launchmultiplayernet`（接管检测目标）。
- 不修改 U3-SDK / Unturned 原生源码 / 独立 LMN 源码；不建立平行库存 RPC。

### 网络层基础（T1 查证）

- `ITransportConnection` 是 Unturned 原生接口（`SDG.NetTransport`），LMN 是消费者；BUE 网络模块构建在其上。
- 接口能力薄：查询方法 + `Send(buffer,size,ENetReliability)` + `CloseConnection()`；命名/版本/可靠 ACK/RPC 全部由 BUE 在接口之上自实现。
- 客户端→服务器方向走 `Provider.clientTransport`（`IClientTransport`，反射），不经 `ITransportConnection`——不对称由 BueNetworkApi 内部屏蔽。
- 实施依赖：刷新 `Libs\SDG.NetTransport.dll` 与游戏安装一致 + 固化 `ITransportConnection` 成员清单作为 BUE 依赖基线。

### BueNetworkApi 契约（T3，Q1-Q12）

- 频道 = FeatureId（一个功能模块一个命名频道），仿 LMN V2 频道路由。
- 版本协商 API 内部自动（Hello/Ack），模块注册时声明 `MinimumBueContract` + 功能版本，不匹配返回 `ContractIncompatible`（复用 SCR-GPT18-001 reason 体系）。
- 可靠性只透传 `Reliable/Unreliable` + 本地发送失败信号；不引入对端应用层 ACK（幂等/事务属模块职责）。
- 会话事件订阅：`IConnectionSession`（SessionId=连接代际 + Connected/Disconnected/GenerationChanged + Send + PeerSteamId + PeerFeatureVersion + Channels 已协商版本表）。
- 发送目标按连接上下文表达（SendToServer / SendToClients / SendToClient(session)），无对端 FeatureId 寻址。
- 错误码：复用 `FeatureRegistrationReason` + 新增 `NetworkSendResult` 显式枚举（可本地化，热路径不抛异常）。
- 公开类型放 `BueNetwork` 命名空间；公开 API 面（接口 + 只读值类型）进 Contracts；帧编解码 / `IClientTransport` 反射 / Steam 细节留 Host 内部；帧格式不进 Contracts（第三方只拿规范实现，不接触帧字节）。

### V1 兼容路径（T4，Q1-Q9）

- V1 兼容 = 旧插件（用 LMN V1 数字频道 API）**无改动运行**；BUE 提供 V1 兼容层模拟旧注册表。
- 帧识别在 API 接收面；"是否启用 V1 兼容"作为官方功能（玩家可在面板关）。
- 旧插件二进制不动：BUE 接管 LMN 注册入口（T5 优先权抢占使旧注册调用落空但兼容层兜底）。
- 退出度量 = 官方功能全 V2 + 已知生态插件迁移比例；**阈值叠加"迁移验证闭环"维度**（LIT/LIR/LHT 源码已迁 V2 但验证未闭环）；V1 兼容长期保留（未知第三方 + 作者自身迁移未验证）。
- V1 兼容层故障只隔离 V1（丢弃 V1 帧 + 诊断），不拖累 V2/本地功能。
- V1 帧结构在 Host 内部代码/注释固化（不进 Contracts）。
- 验收：干净环境装 BUE + 旧 V1 no-op 插件（LMN V1 API 编译），插件不改代码能收发。

### 接管机制（T5，Q1-Q6 + 方向指令）

- 检测：`BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LMN_GUID)` 只读枚举已加载实例（零目录扫描、零 `Assembly.GetTypes()`）；检测时机 = BUE 网络模块初始化时（不依赖 Awake 顺序）。
- 停用：BUE 网络模块对 `NetMessages.ReceiveMessageFromClient/Server` 注册 `Priority.First` Prefix，命中 MOD/LMN2 帧 `return false` 短路；LMN Prefix 永不被调（唯一网络 Hook/频道注册/消息路由）。
- 红线：禁止 `[BepInIncompatibility(LMN_GUID)]`（连坐硬依赖 LMN 的 LIT/LIR/LHT 等插件）。
- 残响语义：接受 LMN Awake 仍执行、静态表仍初始化但路由被短路（仅内存无业务副作用）。
- 面板：显示"已由 BUE 接管"状态 + 「让我改回独立 LMN」按钮（与网络模块可关对齐，可逆性展示）。
- 检测口径：只处理"已加载"实例（DLL 存在但被跳过 = 没在跑 = 不接管）。
- **方向指令（人工）**：LMN / LIT / LIR / LHT 不再作为单独插件存在或单独维护，并入 BUE 内部作为官方功能；玩家只部署单 DLL。

### 配置迁移（T6，Q1-Q5）

- LMN V5 无配置系统 → **空迁移（no-op）**：映射表为空，无文件可破坏、可回滚自动满足。
- 结果记录：结构化日志（`BueRuntimeLog` diagnosticId 惯例）+ 面板一行状态；不持久化"已做过空迁移"标记（幂等空跑）。
- 面板显示一行说明"无独立配置可迁移（LMN 无配置文件）"。
- YAGNI：不预留未来配置键的迁移适配器接口（若 LMN 未来引入配置按 T6 研究重新评估）。
- 协议常数（MOD/LMN2 魔数、GUID、缓存上限）不作为可配置项暴露，由 BUE 帧格式内固化。

### 成熟度定级（T7，Q1-Q4）

- 官方网络模块归 **核心**（BUE"吃掉"LMN 消化为专属网络层），默认启用。
- 与 BueNetworkApi 分开：网络模块（玩家可关）为功能；BueNetworkApi 为核心基础设施（不参与面板开关，常驻低开销）。
- 故障边界拆开：网络层故障**只隔离网络功能**，不触发全局核心安全降级（BII 等本地功能不受影响，U3DS 已验证 BII 不发消息）。

### 生态与前置

- 已发布已知生态（LIT/LIR/LHT）源码 3/3 已迁 V2 但验证未闭环（归档区）→ V2 网络层落地后逐个实机验证（实施前置工作票）。
- 第三方生态接入按 SCR-GPT18-001 注册契约（已冻结）；SDK/文档形态后续定义，方向 = 第三方只拿规范实现。

## Testing Decisions

### 测试原则

- 只测外部行为，不测实现细节；网络层以纯 C# seam 测试（Contracts/Core 不引用引擎类型），真实网络行为以三环境实机验证。
- 红测先行：每个实施票先写失败测试（编译红或运行时红），再实现转绿；沿用仓库现有 `--<ticket>-red` 锚点惯例。

### 模块与测试面

- **BueNetworkApi 契约**（Contracts）：频道注册 / 版本协商 / 会话事件 / 发送结果枚举的纯 C# 契约断言；seam = `ContractTypes.cs` 类型 + 现有 `BetterUnturnedExperience.Contracts.Tests` 运行器。
- **帧路由 / V1 兼容**（Host 内部）：MOD/LMN2 帧识别与短路语义——沿用 T1/T5 反编译证据 + Harmony Prefix 单元测试（仓库现有 Plugin.Tests 模式）；V1 no-op 插件不改代码收发为验收级测试。
- **接管检测**：`Chainloader.PluginInfos.ContainsKey` 语义——BepInEx 运行时事实，测试注入 PluginInfos 快照（现有 `BepInEx.dll` 引用）。
- **设置迁移**：空迁移 no-op 的日志/面板断言（复用 `AssertLogging*` 红测模式与 `DiagnosticLogSink` seam）。
- **面板状态**：接管状态条目 + 停药按钮（`BueNativeManagementPanel` 现有 seam）。
- **三环境验证**：单人 / U3DS / SteamP2PFriends 网络层实机证据绑定 LoadSetIdentity（沿用 DEV-15E/16E 证据包流程）。

### Prior art

- 红测锚点：`tests\BetterUnturnedExperience.Plugin.Tests\Program.cs`（`--logging-*-red` 系列 + `DiagnosticLogSink`/`BueRuntimeLog.Recorder` seam）。
- 契约断言：`BetterUnturnedExperience.Contracts.Tests`。
- 证据门禁：DEV-15E / DEV-16E 三环境证据包 + LoadSetIdentity。

## Out of Scope

- 本地联机、性能优化、背包整理等其他能力迁移（确认稿明确第一阶段不迁移，属后续阶段）。
- 第三方生态功能生产接入的完整 SDK/文档工具链（SCR-GPT18-001 已冻结契约，SDK 形态后续）。
- 独立 Web 前端、HTTP 服务、外部数据库。
- 自动交换 / 自动重排已占用物品。
- 用单一环境证据替代单人/U3DS/P2P 独立验收。
- 运行时插件卸载 / 热重载 / 任意 DLL 扫描（BepInEx 5 不支持运行时卸载，停用走优先权抢占）。
- 本规格不实施生产代码；实施按 /to-tickets 拆 DEV 票 + /implement。

## Further Notes

- 术语：BUE 官方 = 发行版内置并承担维护/资格责任的能力；Unturned 官方（SDG）= 只能消费的原生地基（CONTEXT「Unturned 官方（SDG）」「BUE 官方」）。
- 词汇沉淀已完成：注册阶段 / CatalogRevision / LoadSetIdentity / 功能表现状态 / Launch 系列功能家族 / 客户端间第三方通信平台（CONTEXT.md）。
- 实施依赖清单：SDK 基线锁定；LIT/LIR/LHT V2 迁移验证；网络模块 Settings Facet（仅开关，T7 落地）；BueNetworkApi 类型写入 ContractTypes.cs。
- 证据链：T1/T5/T6/T8 research 报告在 `.scratch/bue-v2-lmn-adoption/research/`；wayfinder 决策在 `.scratch/bue-v2-lmn-adoption/issues/`。
