# RT-05 研究交付与独立审计报告

## 【需求执行概述】

完成后端生命周期、LMN V5、设置持久化、连接代际和客户端/U3DS 引用交集的只读研究；未编写生产代码，未直接修改冻结共享契约。

## 【源码溯源清单】

- BepInEx/LMN 初始化、Harmony、关闭和异常 → `RT-05-Backend-Runtime-Network-Settings-Research.md` §3。
- LMN named channel、reliability、sender、payload、thread、cleanup → §4～5。
- 三类设置存储、迁移、损坏恢复和 overlay → §6。
- capability、SessionReady、降级、SafeMode 与错误族 → §7。
- 内部 adapter seam、拒绝方案和运行证据 → §8～11。
- 接收时连接上下文缺口 → `SCR-RT05-001-receive-time-connection-context.md`。
- SourceSet 排序勘误和 U3DS 引用候选 → `SCR-RT05-002-sourceset-manifest-and-u3ds-references.md`。

## 【变更清单】

- 新增 GPT-RT-05 后端研究报告。
- 新增两项 GPT Shared Contract Change Request。
- 新增交给 Gemini 的 RT-05 同步复核文档。
- 更新 RT-05 为 `ready-for-human` 并更新 `map.md` frontier。
- 未修改任何生产源码、DLL 或冻结契约 token。

## 【验证记录】

- LMN 44 文件 legacy/culture digest：`7151D22EF361B560F44A32963F82CD973AF64D7721F9D109C5492D1D7D864DE6`。
- 同一输入 true ordinal digest：`4F290955FCDA53BFF54E2983BECa4B08337D29266D5C3FB48BD75A4BFA62F2AF`（大小写不影响十六进制值）。
- U3DS Assembly-CSharp 候选 SHA-256：`1AD761B15AD754259BEA8D3053B86105FAB16E09B0A122A101807C87B582060A`。
- U3DS BepInEx 候选 SHA-256：`2674D3AECF3097BEE817ABE7E8BBCC42BF583DF51402069D5FCD4FBED55017CE`。
- 最终研究报告 SHA-256：`C471B69D67B573E1D2EF2AE84CFAA82D26B87300ABA139B5CD659B3E5544E271`。
- 本任务没有生产构建目标；没有用静态研究代替 SP、P2P 或 U3DS 运行证据。

## 【子智能体审核记录】

独立只读审计判定：**PASS（研究交付物）**，0 个内部阻断项。机械复算 SourceSet 摘要、逐文件 hash、候选 DLL hash、stale queued Action 根因、设置非原子行为、四 generation 域和证据分类均一致。

## 【偏离与妥协说明】

无需求偏离。新发现的 U3DS 引用严格标记为 `CANDIDATE_UNFROZEN`；未把它们擅自加入 `BUE-SS-20260824-01`。两个共享缺口只提交 Change Request，未静默实现。

## 【剩余门禁】

- Gemini 对 SessionReady、设置快照、状态投影、降级结果及两个 SCR 的消费复核；
- 人工批准统一 successor SourceSet；
- `SCR-RT05-001` 的方案原型、审计与最终契约裁定。

在上述门禁关闭前，RT-05 不得标记 `resolved`，Ready 后可写设置网络链不得进入生产。


