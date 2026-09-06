# T2：三插件源码盘点与迁入形态

Type: research
Status: resolved（2026-09-06，research 代理盘点完成，主会话入账）
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 无

## Question

盘点 LIT/LIR/LHT 归档源码（`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Archive\2-未闭环验证项目\{LaunchInventoryTidy,LaunchInPlaceReload,LaunchHordeTracker}`），为「迁入 BUE 仓库 + 重写为 BueNetworkApi」提供事实底座：

1. 每插件：项目结构（文件/LOC）、`[BepInPlugin/BepInDependency]` 声明、LMN API 调用面（`ModTransport`/`ModRouter`/`NamespacedTransport` 命名频道注册与发送，逐调用点行号）、游戏/Unity API 面、配置方式（ConfigEntry?）、Harmony/静态状态等迁移风险点；
2. 构建链：csproj 目标框架/引用（DEV-V2-08 kit 已成功重建，`audit/2026-09-05/DEV-V2-08/` 可复用其事实）、版本/CHANGELOG 状态（验收候选 0.0.0）;
3. LMN 命名频道 API → BueNetworkApi 契约面（`src/BetterUnturnedExperience.Contracts/ContractTypes.cs` BueNetwork 命名空间）的映射难度逐插件评估;
4. 迁入形态建议：项目在 BUE 仓库内的落位（目录/csproj 聚合/单 DLL 装配方式）、与 V1 兼容层的边界。

## Answer

报告：`../research/2026-09-06-three-plugin-source-inventory.md`（2026-09-06，逐调用点行号留证）。要点：

- **三插件全是 LMN 命名频道消费方（零 V1 int API）**：频道分别为 LIT `com.yu80rice.launchinventorytidy.net`、LIR `com.yu80rice.launchinplacereload.repack`、LHT `io.github.yu80rice.launchhordetracker.horde-status`。LOC ≈ 14811 / 3105 / 2242；BepInEx 均 0.0.0 + `[BepInDependency(LMN,Hard)]`；无 ConfigEntry。构建事实复用 DEV-V2-08（net472、确定性两轮哈希），未重复实验。
- **契约映射**：`IBueNetworkApi` 能对上 Send/RegisterChannel,但两个缺口——①**公开契约无入站订阅面**（`Subscribe` 仅 Host 内部,第三方消费者收不到帧）;②寻址从 `CSteamID` 改为 `IConnectionSession`。
- **迁移难度排序：LHT < LIR < LIT**。
- **迁入形态推荐**：EmbeddedOfficial 编进单 DLL,走 `IFeatureModule`,不碰 `LmnV1Compat*`。
- **阻塞验证点**（移交 T3/T4-T6）：Subscribe 是否升契约面、生产传输是否已绑、listen-host 双角色、LIT P2P 熔断 scope 调用方消失。
