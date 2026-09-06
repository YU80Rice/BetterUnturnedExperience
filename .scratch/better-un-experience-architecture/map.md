# Wayfinder Map: 更好的未转变者体验架构规划与前后端职责划分

> **SUPERSEDED / 历史输入**：本目录不再作为 Wayfinder 事实源。唯一规范地图为 `../better-unturned-experience-architecture/map.md`；请勿在本文件继续登记决策。

## Destination

完成《更好的未转变者体验》（Better Unturned Experience）公开多创作者协同插件架构与首个核心功能《更好的物品交互》（模糊光标放置与占据网格绿图预览）的前后端职责划分、SDG Glazier 原生 UI 呈现模型、基于 `LaunchMultiplayerNet` 的单向事件流契约及多创作者扩展规范，形成无歧义、已裁定的完整决策包。

## Notes

- **项目名称**：更好的未转变者体验（Better Unturned Experience）
- **首发功能**：更好的物品交互（Better Item Interaction - 物品拖动模糊光标与占据绿图预览）
- **开发环境**：Unturned 3.x, BepInEx 5.x, .NET Framework 4.7.2 / netstandard2.1, SDG Glazier UI
- **网络契约**：基于 `LaunchMultiplayerNet` 强类型命名信道与单向 Command/Event 流，禁止前端直调后端私有逻辑
- **分工与所有权原则**：
  - **前端（Frontend）**：由 **Gemini** 负责。涵盖原生 Glazier UI/HUD、功能设置/开关界面、物品拖拽悬浮预览与占据网格图层呈现、前端 Presenter。
  - **后端（Backend）**：由 **GPT** 负责。涵盖服务端权威校验、网格吸附与冲突计算、BepInEx Harmony 补丁拦截、状态管理、存储与网络同步。
  - **共享契约（Shared Contracts）**：由 **GPT** 统一维护。涵盖双端协议、DTO、事件名、错误码与版本兼容规则。
- **命名与作者标注铁规**：
  - 基础设施文件名保留：`map.md`、`spec.md`。
  - 由 Gemini 撰写的文档、决策票、前端规格与报告统一使用 `Gemini-` 前缀，并在文件内标注 `作者: Gemini`。
  - 由 GPT 撰写的文档、决策票、后端规格与报告统一使用 `GPT-` 前缀，并在文件内标注 `作者: GPT`。
- **关联参考资料**：
  - 演示视频：`E:\下载内容\QQ下载\Screenrecorder-2026-08-24-00-22-52-132.mp4`

## Decisions so far

<!-- 决策索引：每解决一张决策票在此添加一行决策摘要并链接工单 -->

## Open Frontend Tickets (Gemini 负责)

- [Gemini-01: SDG Glazier 原生物品模糊拖拽与绿色占据网格渲染管线](./issues/01-glazier-inventory-preview-rendering.md)
- [Gemini-02: 插件多功能设置与特性开关 UI 生命周期管理](./issues/02-settings-and-feature-toggle-ui.md)
- [Gemini-03: 面向多创作者协同的前端模块注册与拓展标准](./issues/03-multi-creator-frontend-module-registration.md)

## Backend & Shared Contract Tickets (由 GPT 自行编撰与提出)

- **规范事实源说明**：本目录是 Gemini 历史前端工作区；全局唯一决策地图位于 `../better-unturned-experience-architecture/map.md`。GPT-15 已完成双方接口对齐；以下 GPT 文档是后端/契约所有者基线，但本目录自身仍不是全局事实源，也不代表三环境运行通过。
- [GPT 共享契约规格书](../better-unturned-experience-architecture/Shared-Contract-Spec.md)
- [GPT 后端架构规格书](../better-unturned-experience-architecture/Backend-Architecture-Spec.md)
- [GPT-08：定义公共框架最小契约面](../better-unturned-experience-architecture/issues/08-public-contract-surface.md)（resolved）
- [GPT-09：定义模块生命周期与故障隔离状态机](../better-unturned-experience-architecture/issues/09-module-lifecycle-and-isolation.md)（open）
- [GPT-10：定义设置模型、持久化与权威性](../better-unturned-experience-architecture/issues/10-settings-model-and-authority.md)（open）
- [GPT-11：定义网络能力协商与版本不兼容行为](../better-unturned-experience-architecture/issues/11-network-capabilities-and-versioning.md)（open）
- [GPT-12：定义模糊落点与自动旋转算法规格](../better-unturned-experience-architecture/issues/12-item-placement-algorithm.md)（open）
- [GPT-13：定义前后端交接状态与失败反馈](../better-unturned-experience-architecture/issues/13-frontend-backend-handoff.md)（open）
- [GPT-14：定义开放协作、构建与三环境发布门禁](../better-unturned-experience-architecture/issues/14-contribution-and-release-gates.md)（open）
- [GPT-15：对齐 Gemini 前端输入与唯一决策地图](../better-unturned-experience-architecture/issues/15-reconcile-frontend-input.md)（resolved）

## Not yet specified

<!-- 迷雾区域（Fog of War）：已知在视野内但在前沿决策解决前尚无法精准下钻的议题 -->
- 多创作者在 GitHub 提交自定义交互模块时的 CI 自动化校验与命名空间隔离规则
- 极端弱网高延迟（RTT > 250ms）或丢包场景下，物品拖动放置的本地预测与服务端 Rollback 表现层平滑插值
- 针对模组自定义超大尺寸异形背包/异形容器（如非矩形网格或带独立插槽装备）的通用占据算法适配

## Out of scope

<!-- 超出本次规划目标的事项 -->
- 独立 Web 端管理后台或引入外部第三方 HTTP/Web 服务（已明确排除，全内嵌于 Unturned 原生网络）
- 重写 Unturned 原版专用服务端（U3DS）全图物理引擎
- 脱离 SDG Glazier 原生 UI 体系引入重量级外部 UI 渲染框架


