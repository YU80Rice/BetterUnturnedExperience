# V1 数字频道生态插件盘点

Type: wayfinder:research
Status: open
Parent: V2 第一阶段：LMN 官方纳入与 BueNetworkApi（Wayfinder 地图）
Blocked by: 无（与 T5 并行）

## Question

LMN 生态中实际使用 V1 数字频道（`virtualChannel` int 频道）的插件有哪些？它们的迁移状态如何？——为 T4 Q4 的"已知生态插件迁移比例阈值"提供数据基线。

## 查证要点

1. 从 LMN 源码（`D:\Agent-工作目录\DevelopMyUNMultiplayerModAndModloader\LaunchMultiplayerNet`）识别 V1 数字频道 API 的调用面（`ModTransport.SendViaTransport(ITransportConnection, int virtualChannel, ...)`、`ModRouter` 等），确定"什么算用 V1"的可判据。
2. 从 U3-SDK / 启动器目录 / Steam 创意工坊等来源盘点已知的 LMN 生态插件（依赖 LaunchMultiplayerNet.dll 或使用其 V1 数字频道 API 的 BepInEx 插件）：名称、仓库/来源、是否仍活跃、是否已迁移 V2 命名频道。
3. 若无法穷举（生态可能很大），给出"可判据 + 已知样本 + 迁移状态未知项清单"，让阈值可基于已知样本计算并明确其局限。
4. 证据：来源引用 + 路径；必要时用 browser-skill 查创意工坊/GitHub。

## 答案

（resolved 时记录生态清单 + 迁移比例基线）
