# BUE 开发者入口（给人看的那一份）

> 第一次给 BUE 写生态插件的作者、想看懂仓库组织的贡献者，从这里进。
> **这里不是契约。** 契约的唯一事实源是 [BUE 开发者契约（SDK）](../sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md)；
> 本目录只解释「平台怎么搭、先看哪里、为什么这样接」。与 SDK 不一致的地方，一律以 SDK 为准。

## 文档分四层，各管各的读者

| 层 | 读者 | 位置 | 管什么 |
|---|---|---|---|
| 玩家手册 | 玩家 | [docs/BetterUnturnedExperience-Player-Handbook.md](../BetterUnturnedExperience-Player-Handbook.md) | 安装、升级、防双装、故障自查 |
| 开发手册（本目录） | 生态作者 / 人类贡献者 | [./BetterUnturnedExperience-Developer-Handbook.md](BetterUnturnedExperience-Developer-Handbook.md) | 一张总图 + 三短章：模块结构 / 最小接入流程 / HelloFeature 与探针导读 |
| SDK 契约 | 生态作者（动手与发布前对照） | [docs/sdk/](../sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md) | 身份冻结面、承诺与不承诺、接入六步原文、逐缝语义、码表、自检清单 |
| 阶段规格与票面 | AI / 维护代理 | 工作副本的本地工单（不在公开 clone） | 「为什么这样设计」的决策记录；不面向人阅读，也不是契约 |

查词去仓库根部的 [CONTEXT.md](../../CONTEXT.md)（领域词汇表）。本手册不教装游戏——安装问题看玩家手册。

## 推荐阅读顺序

1. **[开发手册](BetterUnturnedExperience-Developer-Handbook.md)**——先花十分钟看完一张总图和三短章，建立「谁装什么、模块几时活几时退、我从哪个缝接进去」的整体图景；
2. **[HelloFeature.cs](HelloFeature.cs)**——把约四十行的独立插件骨架整段复制进自己的工程，先完成硬依赖、公开桥登记和空的开工收工；
3. **[SDK §1 与 §4](../sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md)**——动手前读权威原文：适用范围、承诺与不承诺、生态 DLL 接入六步；
4. **[NoOp 合规探针源码](../../src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs)**——只有在需要逐缝验证宿主行为时再读这份测试资产；
5. **发布前**——回到 SDK：用到哪条平台服务就查附录 A 对应小节，出货前过附录 C.6 的上架前自检清单。

## 三条权威规则（先记住）

- **注册与运行平台能力**：以 SDK 为准。手册是解释，不是第二份契约；
- **最小实现的姿势**：先复制 [HelloFeature.cs](HelloFeature.cs)，只保留自己的身份、硬依赖、公开桥登记和空的开工收工；需要逐缝验证时，再读 NoOp 合规探针；
- **设计缘由**：在本地工单与规格里，那是给 AI 与维护代理看的执行规格，不要当契约引用；公开 clone 不带这些文件。

## 这个目录里有什么

- [README.md](README.md)（本页）——入口与阅读路线；
- [BetterUnturnedExperience-Developer-Handbook.md](BetterUnturnedExperience-Developer-Handbook.md)——一张总图 + 三短章的正文；
- [HelloFeature.cs](HelloFeature.cs)——可整段复制的最小接入骨架；
- [NoOpFeaturePlugin.cs](../../src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs)——需要测缝时使用的合规探针。

再往深处，一律回到 SDK；探针只用于逐缝验证。
