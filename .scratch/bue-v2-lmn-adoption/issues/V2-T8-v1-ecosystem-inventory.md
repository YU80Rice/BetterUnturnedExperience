# V1 数字频道生态插件盘点

Type: wayfinder:research
Status: resolved（2026-09-03 盘点完成，报告落盘）
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

- **V1 判据（可执行）**："用 V1" = 对 LMN 调用 `int virtualChannel` 形态 API（`ModTransport.Register*Handler(int,...)` / `SendToServer|SendToClient|BroadcastToAllClients(int,...)` / `BuildMessage(EModMessage,...)`）或经由 `MOD` 魔数帧；仅 `[BepInDependency]` 编译期引用不算 V1 消费方（`Routing/ModTransport.cs:137,159,186,216,415,456,495,818`；`Routing/ModRouter.cs:13-16,40-51,73-107`）。
- **已知生态 = YU80Rice 单作者 mod 家族**（LMN + 全部发布消费方 git remote 均 `github.com/YU80Rice/*`；本机无外部第三方下游）。
- **迁移基线（2026-09-03 作者补充修正）**：LIT(100)/LIR(101)/LHT(102) 的**当前源码全部使用 LMN V5 V2 命名频道 API**（`RegisterNamed*`/`SendNamed*`），并有 v3.0.0/v3.0.1 发布提交（LIR 迁移提交 `a6b5964`、LHT `a069c17`、LIT `24a3333`）——源码口径 3/3 已迁 V2。**但作者确认：这三个迁移从未经过具体测试验证，因此被归档在 `Archive\2-未闭环验证项目`（未闭环区）**。→ "已发布已知生态"口径修正为"**源码已迁 + 发布提交存在，验证未闭环**"，不能视为已验证迁移。唯一仍用 V1 的 LaunchSecureContainer（频 103）未发布、无部署、无 git。
- **局限（显式声明）**：非穷举——外部/线下/创意工坊未知第三方无法枚举（bsk 无浏览器、web_search 无有效信号）；阈值 = **已知样本下限基线**。
- 完整报告：`research/V2-T8-v1-ecosystem-inventory.md`。
- **给 T4（修正后）**：源码口径迁移比例 100%，但**验证未闭环**——退出阈值不能仅凭"源码已迁"判定，须叠加"迁移验证闭环"维度；V1 兼容层保留理由由"未知第三方"+"作者自身迁移未验证"共同承担。
