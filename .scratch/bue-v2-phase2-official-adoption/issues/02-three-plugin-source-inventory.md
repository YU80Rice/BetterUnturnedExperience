# T2：三插件源码盘点与迁入形态

Type: research
Status: open
Parent: map.md（三插件官方纳入与平台首公里）
Blocked by: 无

## Question

盘点 LIT/LIR/LHT 归档源码（`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Archive\2-未闭环验证项目\{LaunchInventoryTidy,LaunchInPlaceReload,LaunchHordeTracker}`），为「迁入 BUE 仓库 + 重写为 BueNetworkApi」提供事实底座：

1. 每插件：项目结构（文件/LOC）、`[BepInPlugin/BepInDependency]` 声明、LMN API 调用面（`ModTransport`/`ModRouter`/`NamespacedTransport` 命名频道注册与发送，逐调用点行号）、游戏/Unity API 面、配置方式（ConfigEntry?）、Harmony/静态状态等迁移风险点；
2. 构建链：csproj 目标框架/引用（DEV-V2-08 kit 已成功重建，`audit/2026-09-05/DEV-V2-08/` 可复用其事实）、版本/CHANGELOG 状态（验收候选 0.0.0）;
3. LMN 命名频道 API → BueNetworkApi 契约面（`src/BetterUnturnedExperience.Contracts/ContractTypes.cs` BueNetwork 命名空间）的映射难度逐插件评估;
4. 迁入形态建议：项目在 BUE 仓库内的落位（目录/csproj 聚合/单 DLL 装配方式）、与 V1 兼容层的边界。

## Answer

（research 代理填：结论摘要 + 报告链接）
