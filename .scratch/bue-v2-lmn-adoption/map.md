# V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）

Type: task
Status: charted（2026-09-03，7 张子票已建立）
Label: wayfinder:map
Parent: 无（V2 开放运行时平台方向，来自 CONTEXT.md 已冻结的 BUE V2 产品方向）
Author: GPT（本会话 charting）

## Destination

形成一份可交给 `/to-tickets` 的 **V2 第一阶段完整规格**：BUE 内置 `BueNetworkApi`、V2 命名频道可用、V1 旧插件兼容路径仍可运行、BUE/独立 LMN 共存接管生效、配置迁移完成、单人/U3DS/SteamP2PFriends 三环境网络层验证通过、网络模块故障不影响本地功能、迁移资料与开发文档齐备（CONTEXT.md「LMN 纳入完成标准」8 条全满足）。

## Notes

- 领域：Unturned（U3-SDK 2022.3.62）+ BepInEx 5.4.23.5 + LMN（LaunchMultiplayerNet，本地联机能力）官方纳入为 BUE 网络模块；不接管原版协议包；V1 数字频道只保留旧插件兼容、不再提供新 V1 注册入口。
- 前置契约：`SCR-GPT18-001`（外部功能注册/LoadSet 契约）已 Gemini ACCEPT，待人工批准后冻结，作为开放平台注册骨架。
- 每张票解决一个决策，不实施生产代码（charting/grilling 阶段）；research 票可并行。
- 技能：grilling + domain-modeling（每次决策必调）；research（查证）；prototype（接管机制等"怎么做"问题）；browser-skill（查证 LMN 内部实现、Unturned U3-SDK 源码、Forge 功能细节时用）。
- 玩家侧始终单 DLL `BetterUnturnedExperience.dll`；官方功能与第三方功能平权（同一注册/生命周期路径）。

## Decisions so far

- [SCR-GPT18-001 契约批准与冻结](issues/V2-T2-scr-gpt18-001-approval.md)：人工开发者 2026-09-03 批准并冻结外部功能注册/LoadSet 契约（Gemini ACCEPT + GPT 审计 PASS 背书）；契约写入 `ContractTypes.cs` 另立 DEV 工单实施；T4 V1 兼容路径阻塞解除。
- [ITransportConnection 真实形态查证](issues/V2-T1-itransportconnection-shape.md)：`ITransportConnection` 是 Unturned 原生接口（`SDG.NetTransport`），LMN 是消费者；BUE 自建网络层可行（高级语义需自实现）；客户端→服务器方向走 `IClientTransport`（不对称）。完整报告 `research/V2-T1-itransportconnection-shape.md`；解锁 T3、T5。
- [官方网络模块成熟度定级](issues/V2-T7-network-module-maturity-tier.md)：网络模块归**核心**（BUE"吃掉"LMN 消化为专属网络层）；与 BueNetworkApi 分开定级（API=核心基础设施，不参与面板开关）；故障边界拆开（网络层故障仅隔离网络功能，不触发全局核心安全降级——BII 等本地功能不受影响，U3DS 已验证）。
- [BueNetworkApi 契约设计](issues/V2-T3-buenetworkapi-contract.md)：两轮 grilling 冻结 API 形状——频道=FeatureId（Q1）、版本协商 API 内部自动（Q2）、只透传可靠性+本地失败信号（Q3）、会话事件订阅（Q4）、双向链路抽象进 API（Q5）、公开 API 面进 Contracts / 帧格式留 Host 内部（Q6）、独占接收面 + V1 兼容接收路径（Q7，V1 数字频道只兼容不注册）、无对端 FeatureId 寻址（Q9）、会话含 SteamId/版本/频道表（Q10）、NetworkSendResult 显式枚举（Q11）、`BueNetwork` 命名空间（Q12）。解锁 T5。
- [V1 兼容路径设计](issues/V2-T4-v1-compat-path.md)：V1 兼容 = 旧插件**无改动运行**（Q1）；帧识别在 API 接收面 + 兼容策略为官方功能可关（Q2）；BUE 接管 LMN 注册入口、旧插件二进制不动（Q3）；退出度量 = 官方全 V2 + 已知插件迁移比例（Q4）；V1 兼容层故障只隔离 V1（Q5）；V1 帧结构代码内固化（Q6）；验收 = 旧 V1 no-op 插件不改代码能收发（Q8）；依赖 T5 先接管（Q9）。**T8 回填（含作者修正）：LIT/LIR/LHT 源码已迁 V2 但验证未闭环 → 退出阈值须叠加"迁移验证闭环"维度，V1 兼容长期保留。**
- [V1 数字频道生态插件盘点](issues/V2-T8-v1-ecosystem-inventory.md)：V1 判据可执行（`int virtualChannel` API 调用面）；已知生态 = YU80Rice 单作者 mod 家族；LIT/LIR/LHT 源码 3/3 已迁 V2（发布提交存在）但**作者确认未测试、归档未闭环区**；唯一仍用 V1 的 SecureContainer 未发布；非穷举（外部/创意工坊未知项显式声明）。完整报告 `research/V2-T8-v1-ecosystem-inventory.md`。
- [独立 LMN 共存接管机制](issues/V2-T5-lmn-coexistence-takeover.md)：检测 = `Chainloader.PluginInfos.ContainsKey(LMN_GUID)`（零扫描）；停用 = Harmony `Priority.First` 前缀抢占 `NetMessages.ReceiveMessageFromClient/Server`（唯一可行，LMN Prefix 永不被调）；`BepInIncompatibility` 红线禁止（连坐硬依赖插件）。grilling 拍板：全帧接管（Q1）、接受残响（Q2）、禁连坐（Q3）、面板带停药按钮（Q4）、只处理已加载（Q5）、不需排序依赖（Q6）。**用户方向：LMN/LIT/LIR/LHT 并入 BUE 官方功能，不再独立维护**。完整报告 `research/V2-T5-lmn-takeover-mechanism.md`。解锁 T6。

## Not yet specified

- 本地联机、性能优化、背包整理等能力迁移（确认稿明确第一阶段不迁移，属后续阶段）。
- 第三方生态功能生产接入的完整工具链（SCR-GPT18-001 冻结后的 SDK/文档形态——T3 Q8 已定方向：第三方只拿规范实现，不接触帧字节）。
- V1 兼容过渡期的具体退出时点（由迁移覆盖率与生态准备程度决定，非日期；T4 Q4 已定度量方式，阈值待 T8 数据）。
- **实施依赖（T1 带入，随 T3 毕业）**：`Libs\SDG.NetTransport.dll` 与游戏安装二进制漂移的 SDK 基线锁定 + `ITransportConnection` 成员清单固化。

## Out of scope

- 独立 Web 前端、HTTP 服务、外部数据库（已有 map.md Out of scope，继续排除）。
- 自动交换 / 自动重排已占用物品（V1 map.md 已排除）。
- 用单一环境证据替代单人/U3DS/P2P 独立验收（三环境边界不放松）。
- 本 effort 不实施生产代码；只形成决策与规格。
