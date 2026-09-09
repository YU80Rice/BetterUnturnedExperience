# Better Unturned Experience（更好的未转变者体验）

> **这是一个 vibecoding 作品。**
> 作者只是一名热爱 Unturned 的普通玩家，不是科班出身的开发者——这个项目是带着热情，在与 AI 结对中边学边写、一点点长起来的。它一定还有许多不成熟、不规范的地方：无论是架构上的批评、实现上的指正，还是「这里其实可以这样做」的一句提点，都恳请各位开发者不吝赐教。作者会虚心接受每一条意见，认真把它改得更好——也希望这个小小的工具，真的能让大家在 Unturned 里的体验更舒服一点。

基于 BepInEx 5.4.23.5 的 Unturned 体验改进与模组平台。**单 DLL 交付**：玩家只装 `BetterUnturnedExperience.dll` 一个文件，即可在三种环境（单人 / SteamP2PFriends 好友联机 / U3DS 独立服务器）获得四个官方功能。

## 功能一览

| 功能 | 说明 |
|---|---|
| 更好的物品交互（BII） | 背包内直接拖拽物品，拖拽预览与保活由 BUE 接管 |
| 背包整理（LIT） | 整理按钮 / Ctrl+点击一键整理全身 / 同类·空间·大件三种模式 |
| 更好的换弹体验（LIR） | 持枪双击换弹键一键压弹（toast 显示压入发数），整理后自动压弹 |
| 更好的尸潮播报（LHT） | 服务器管理员 `/horde` 启动尸潮，全体玩家 HUD 实时显示爆发地点与剩余数 |

- 管理面板（G）四功能中文名条目、逐功能开关；
- 与独立 LMN（LaunchMultiplayerNet）可共存，兼容依赖 V1 数字频道的旧插件（**裸 BUE 下 V1 数字频道旧插件不收发=已承认边界**，详见玩家手册）；
- 防双装：标准 BepInEx 装载路径由引擎按程序集身份折叠副本；非标准装载路径进来的副本触发 `BUE-PLATFORM-001` 警示（只提示、永不改文件）。

## 当前版本

- **v8 —— V2 第二阶段正式交付版**（2026-09-09，阶段工单 14..25 全部闭环）
- `BetterUnturnedExperience.dll` SHA-256：`F7B7513C569B8D2830CCDBF7BB4AB0C9E0B6ABDED88B2708E0A9F3B8303DF569`（548352 字节，三轮确定性重建逐字节一致）
- 身份与门禁台账：[`audit/RELEASES.md`](audit/RELEASES.md) 行 10；正式交付包：[`publish/第2阶段-正式交付版本/`](publish/第2阶段-正式交付版本/)

## 快速开始

1. 安装 BepInEx 5.x 前置；
2. 把 `BetterUnturnedExperience.dll` 放进 `BepInEx/plugins/`（客户端与 U3DS 服务器**各一份**）；
3. 启动游戏即可。

详细安装与升级（旧部署迁移、独立 LMN 保留语义、U3DS 要点、故障自查三步）见 **[玩家手册](docs/BetterUnturnedExperience-Player-Handbook.md)**。

## 给生态开发者

BUE 的功能分两层：**官方功能**（上表四件）是源码模块，构建期聚合进唯一主 DLL；**生态功能**是您交付的独立 BepInEx 插件 DLL——BUE 扮演您插件的前置库与运行时平台。

- **开发者契约（唯一事实源）**：[`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`](docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md)——程序集身份冻结面与三段式承诺、编译期引用规则（引用主 DLL、`CopyLocal=false`、禁止捆绑）、防双装处置、公开注册桥 `BueRuntimeHost.Register` 用法、契约版本演化登记；
- **生态活样板**：[`src/BetterUnturnedExperience.NoOpFixture/`](src/BetterUnturnedExperience.NoOpFixture/)（独立 GUID + HardDependency 前置 + 经公开桥注册，被宿主测试长期验证）；
- **平台 API 面**：`BueNetworkApi`（方向订阅/会话/定向与会话驱动组播）、功能事件（`TidyCompleted`）、宿主时钟（`HostTick`）、设置、诊断与隔离缝。

## 仓库结构

```
src/        源码（Plugin 宿主 / Core / Contracts / Lit / ClientUi / NoOpFixture 样板 …）
docs/       玩家手册、SDK 契约、attribution 署名、ADR 架构决策
audit/      发布台账 RELEASES.md + 各工单审计证据（哈希/红绿链/实机验收）
publish/    正式交付包（按阶段归档）
.scratch/   本地工单与规格（issue tracker）
```

## 来源与致谢

### 官方功能前身（作者自研项目，源码已迁入本仓库为单事实源）

| 前身项目 | 开源仓库 | 去向 |
|---|---|---|
| LaunchInventoryTidy | [github.com/YU80Rice/LaunchInventoryTidy](https://github.com/YU80Rice/LaunchInventoryTidy) | 迁入 → LIT 背包整理 |
| LaunchInPlaceReload | [github.com/YU80Rice/LaunchInPlaceReload](https://github.com/YU80Rice/LaunchInPlaceReload) | 迁入 → LIR 更好的换弹体验 |
| LaunchHordeTracker | [github.com/YU80Rice/LaunchHordeTracker](https://github.com/YU80Rice/LaunchHordeTracker) | 迁入 → LHT 更好的尸潮播报 |
| LaunchMultiplayerNet | [github.com/YU80Rice/LaunchMultiplayerNet](https://github.com/YU80Rice/LaunchMultiplayerNet) | 独立保留（BUE 与其共存 + V1 数字频道兼容层） |

各迁入项目的逐项署名见 [`docs/third-party/`](docs/third-party/)。

### 第三方项目致谢

- **[UnturnedPluginManager](https://github.com/35117/UnturnedPluginManager)**（作者 35117）：BUE 管理面板的插件列表、设置展示与 UI 重建检测经验来源。BUE 运行时不依赖其 DLL、不复用其插件身份；来源、采用提交与授权记录详见 [`docs/third-party/UnturnedPluginManager-attribution.md`](docs/third-party/UnturnedPluginManager-attribution.md)。

### 作者的工具链

自研启动器 **LaunchP2PHostManager（UMM）**：[github.com/YU80Rice/LaunchP2PHostManager](https://github.com/YU80Rice/LaunchP2PHostManager)——P2P 联机启动器与诊断包制作工具。

## 许可证

本项目以 **[MIT License](LICENSE)** 开源（`Copyright (c) 2026 YU80Rice`）；自 Launch 系列迁入的源文件保留其 MIT 版权与许可声明头。

**免责声明**：本项目与 Unturned 官方（Smartly Dressed Games）无关联；本仓库不含任何游戏本体资产；Unturned 游戏本体版权归 Smartly Dressed Games 所有。
