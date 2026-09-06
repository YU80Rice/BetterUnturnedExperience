# LaunchInventoryTidy（背包整理）来源与署名声明

## 来源

- 项目：`LaunchInventoryTidy`（Launch 系列三插件之一）
- 本地来源快照：`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\Archive\2-未闭环验证项目\LaunchInventoryTidy`
- 采用时版本：`0.0.0` 验收候选（DEV-V2-08 生态验证 kit 绑定的 LIT 候选，SHA-256 `7e35d7c7…5417`）
- 作者署名：`YU80Rice`
- 许可证：MIT License（原项目 `LICENSE` 文件，Copyright (c) 2026 YU80Rice）

## 纳入方式

2026-09-06，DEV-V2-15（V2 第二阶段·三插件官方纳入）按 wayfinder 拍板的「源码迁入本仓库为单事实源，原 Archive 仓库停维护」决策，将 LIT 领域源码迁入 `src/BetterUnturnedExperience.Lit/` 并聚合进单一玩家 DLL。迁入文件保留 MIT 版权与许可声明头；行为按「算法原样迁移」口径迁移（排序与放置行为不变），仅按本仓库纪律改写宿主身份（删除 BepInEx 插件身份与 LMN 硬依赖）、日志接缝与 DEV-V2-15 明示的 seams（`ITidyStrategy`、模块生命周期、设置权威）。

## BUE 中的使用方式

运行时不依赖 `LaunchInventoryTidy.dll`，也不复用其 BepInEx 插件身份 `com.yu80rice.launchinventorytidy`；功能身份为 `io.github.yu80rice.bue.inventory-tidy`（面板条目「背包整理」）。旧命名频道随原插件退役；联机传输形态由 DEV-V2-21 按公开契约 `IBueNetworkApi` 重建。

## 致谢

感谢 `YU80Rice` 编写并验收 LaunchInventoryTidy 的事务化整理实现（Prepare/Commit/Verify/回滚 + mutation journal + 快捷键迁移），BUE 官方纳入直接吸收该实现。
