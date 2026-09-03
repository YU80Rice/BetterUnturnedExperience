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

## Not yet specified

- 本地联机、性能优化、背包整理等能力迁移（确认稿明确第一阶段不迁移，属后续阶段）。
- 第三方生态功能生产接入的完整工具链（SCR-GPT18-001 冻结后的 SDK/文档形态）。
- V1 兼容过渡期的具体退出时点（由迁移覆盖率与生态准备程度决定，非日期）。
- BueNetworkApi 是否需要公开订阅/事件流原语（等 T1 查证原生传输能力后定）。
- **T1 遗留待格审项（从研究毕业）**：接收面所有权边界（BUE 接管 vs 叠加 LMN 拦截）；客户端 `IClientTransport` 不对称纳入设计；`ENetReliability` 无交付反馈的可靠性边界；`Libs\SDG.NetTransport.dll` 与游戏安装二进制漂移的 SDK 基线锁定；`ITransportConnection` 接口成员清单作为 BUE 依赖文档。

## Out of scope

- 独立 Web 前端、HTTP 服务、外部数据库（已有 map.md Out of scope，继续排除）。
- 自动交换 / 自动重排已占用物品（V1 map.md 已排除）。
- 用单一环境证据替代单人/U3DS/P2P 独立验收（三环境边界不放松）。
- 本 effort 不实施生产代码；只形成决策与规格。
