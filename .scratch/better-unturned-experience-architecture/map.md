# 更好的未转变者体验：前后端职责与可扩展架构决策地图

> 基础设施文件；当前内容由 GPT 撰写。

## Destination

形成一套可交给 `/to-spec` 的完整架构决策包：明确公共框架、前端、后端、共享契约和功能模块的职责，确定单 DLL 模块化构建、兼容策略与三环境验收边界，并以“更好的物品交互”作为首个参考实现。

## Notes

- 平台：BepInEx 下的《未转变者》插件，不引入 Web 前端或 Web 服务。
- 受支持环境：单人、SteamP2PFriends、U3DS；SteamP2PFriends 承担本地联机能力，不另设原生 Listen Host 场景。
- GPT 负责后端与共享契约；Gemini 负责前端。共同接口变更需要双方复核。
- 固定基础设施文件保留技能要求的名称，并注明作者；GPT/Gemini 各自产出的其他文档使用对应前缀。
- 本文件是当前 effort 的唯一规范决策地图。Gemini 已通过 GPT-15 对当前 GPT Shared/Backend 与 Gemini Frontend 规格完成事后逐项复核；`.scratch/better-un-experience-architecture/` 仅作为历史工作区，其中未被当前票据明确接受的草案内容不构成全局架构事实。
- Wayfinder 决策包已通过联合一致性复审。统一需求规格采用双语镜像发布：[英文 Agent 执行版](spec.md) 与 [中文人工阅读版](spec.zh-CN.md)，均分为共享契约、Gemini 前端 U3-SDK 调研和 GPT 后端 U3-SDK 调研三层；人工开发者已明确确认三层最高测试 seam，当前状态为 `ready-for-agent`。两份规格必须在同一变更中同步，英文精确契约签名不得被翻译。下一步仅授权进行 `/to-tickets` 分解与研究准备；生产实现、构建与环境验收仍未开始。
- 每次处理一个非研究决策票；研究票可以并行。
- 处理领域语言时使用 `/domain-modeling`；需要用户决策时使用 `/grilling`；技术事实研究使用 `/research`。

## Decisions so far

- [完成 DEV-16 真实运行时接线规格](spec-DEV-16-runtime-clientui-management-panel.md)：BUE 最终只发布单一主 DLL，内化 `UnturnedPluginManager` 的管理面板能力并保留作者/仓库/提交/许可记录；冻结 Client/Headless 分流、原生库存 Adapter、BUE Settings Facet、普通 ConfigEntry 兼容编辑、收藏与排序、主菜单/暂停菜单入口，以及 DEV-16A～DEV-16E 的实施顺序。

- [确定产品终点与首版范围](issues/GPT-01-product-scope.md)：交付可用公共框架、开发规格、统一设置入口和模块注册机制，并以“更好的物品交互”作为首个参考实现。
- [划定前端、后端与共享契约所有权](issues/GPT-02-role-boundaries.md)：Gemini 负责玩家可见交互，GPT 负责后端及版本化共享契约。
- [确定模块化、兼容与故障隔离原则](issues/GPT-03-modularity-compatibility.md)：功能独立工程、构建合并为单 DLL，模块故障局部隔离，公共 API 遵守语义化兼容。
- [确定更好的物品交互核心行为](issues/GPT-04-item-interaction-behavior.md)：后端确定性计算候选落点，支持自动旋转，不自动交换，失败时保留原位置，并由原生权威路径提交。
- [查明 Unturned 与 BepInEx 运行时技术基线](issues/GPT-05-runtime-baseline-research.md)：客户端与 U3DS 的 BepInEx 和 `Assembly-CSharp` 基线不同，工程暂以 .NET Framework 4.7.2 与 C# 10 为起点，但必须限制在双端 API 交集并独立验收。
- [选择独立功能工程合并为单 DLL 的构建方案](issues/GPT-06-single-dll-build-research.md)：首版采用模块独立工程、模块自有共享源码清单与单一聚合工程一次编译；ILMerge 不进入方案，ILRepack 仅保留为未来受控实验。
- [查明原版库存移动与多人权威调用链](issues/GPT-07-inventory-authority-research.md)：增强范围限定为客户端候选预览，最终沿用原版 `sendDragItem → ReceiveDragItem` 服务端权威链路，三环境行为仍需同哈希运行验证。
- [定义公共框架最小契约面](issues/GPT-08-public-contract-surface.md)：公共 interface 限于模块、状态、设置、能力、日志与进程内候选 DTO；LMN 是能力/设置/诊断 adapter，库存提交继续走原版权威链路，逐次成功/拒绝事件不进入 V1 网络契约。
- [定义模块生命周期与故障隔离状态机](issues/GPT-09-module-lifecycle-and-isolation.md)：首次越界异常即隔离；模块 Stop 与核心资源登记簿双保险清理；依赖按拓扑启动、逆拓扑级联；核心不变量失败进入 SafeMode。
- [对齐 Gemini 前端输入与唯一决策地图](issues/GPT-15-reconcile-gemini-frontend-input.md)：Gemini 已逐项接受当前 Draft interface 和权威边界；具体 UI、算法与超时留在 Gemini-03、GPT-12、GPT-13，不升级为 Stable。
- [定义设置模型、持久化与权威性](issues/GPT-10-settings-model-and-authority.md)：冻结三种设置权威、每功能/作用域原子 revision、分作用域持久化与迁移、连接会话覆盖及前端完整快照消费契约。
- [定义网络能力协商与版本不兼容行为](issues/GPT-11-network-capabilities-and-versioning.md)：冻结 LMN/应用双层握手、按功能能力交集降级、原版连接不受阻、连接代际与 nonce、有界原子分片及最小状态投影。
- [定义模糊落点与自动旋转算法规格](issues/GPT-12-item-placement-algorithm.md)：冻结 Local-Fit Priority，按局部当前、局部旋转、全局当前、全局旋转阶梯消除空地翻转蠕动并保留遇阻智能旋转。
- [定义前后端交接状态与失败反馈](issues/GPT-13-frontend-backend-handoff.md)：冻结 AwaitingProjection、原生投影关联、设置重试/快照恢复、网络静默降级、模块 UI 矩阵与通知去重。
- [定义开放协作、构建与三环境发布门禁](issues/GPT-14-contribution-and-release-gates.md)：冻结领域片段/Definition Linker/单一定义产物、贡献治理、CandidateBuild、证据案例、技术资格与发布授权；独立审计、Gemini 与人工复核均已通过。
- [裁定参考功能模块与最小接入样例](issues/GPT-16-reference-feature-module.md)：不新增第二个产品功能；“更好的物品交互”继续作为唯一首发参考实现，轻量 sample 仅可在实施阶段作为不进入 DLL 的文档/测试夹具。
- [联合复核前后端 Wayfinder 决策包](issues/GPT-17-joint-wayfinder-consistency-review.md)：JCR-01～09 全部关闭；旋转方向、两类坐标区间、intended center seam 与证据边界完成双端对齐，独立终审 PASS。
- [完成 DEV-15C Projection Relay 关闭修复](issues/DEV-15C-projection-relay-awaiting-projection.md)：补齐 `INativeInventoryProjectionSource.TryCapture` 直接消费测试；Release、全套测试、静态门禁与独立审计通过，Gemini R1 `ACCEPT`，本票已 `resolved`。
- [完成 DEV-15D Settings + Lifecycle + Isolation 构建](issues/DEV-15D-settings-lifecycle-isolation.md)：设置快照门禁、九态生命周期、SafeMode、局部隔离、卫星降级、原生回退与 surface 重绑失效已实现；GPT R3 独立审计 PASS，Gemini 前端消费复核 ACCEPT，本票已 resolved。
- [完成 DEV-15E Qualification Evidence 自动化门禁构建](issues/DEV-15E-qualification-evidence.md)：证据包校验、同候选三环境资格组合与 Fail-Closed 政策已实现；Release/7 项测试/Contracts-Core 扫描 PASS，GPT 独立审计返修后 PASS；真实 SP/P2P Host/Client/U3DS 证据仍待人工采集，票据保持 ready-for-human。
- [建立 DEV-15E-HUMAN 真实运行证据采集门禁](issues/DEV-15E-HUMAN-runtime-evidence.md)：锁定当前 BUE 主 DLL SHA-256，规定 SP、P2P Host/Client、U3DS 的同哈希、CaseId、时间窗和原始日志要求；未完成前不宣称 DEV-15 整体运行或发布通过。
- [完成 DEV-16 真实运行时接线规格](spec-DEV-16-runtime-clientui-management-panel.md)：冻结 BUE 单 DLL 内化管理面板、主插件 Runtime Composition Root、真实 ClientUi/原生库存 Adapter、BUE Settings Facet、ConfigEntry 兼容编辑、收藏排序、主菜单/暂停菜单入口和 U3DS Headless 隔离；总工单已标记 `ready-for-agent`，下一步为 `/to-tickets`。
- [拆分 DEV-16A～DEV-16E 实施工单](issues/01-dev-16a-single-dll-composition-root.md)：按依赖顺序建立单 DLL 组合根、管理面板/设置、原生库存生命周期、拖拽预览/投影和三环境证据五个垂直切片；01 可立即开始，02/03 并行阻塞于 01，04 阻塞于 02/03，05 阻塞于 01～04。
- [完成 DEV-16A 单 DLL Composition Root](issues/01-dev-16a-single-dll-composition-root.md)：主 DLL 已嵌入 Contracts/Core/ClientUi Seam，官方 ClientUi Satellite 非空，客户端/Headless/不可用门禁、幂等初始化与销毁隔离通过；Release 编译、7 项测试、静态扫描和独立审计 PASS。真实 Glazier/Sleek/Harmony/库存接线仍属于 DEV-16B～D。

## Not yet specified

### Requirements research ticket distribution

- [RT-01：冻结共享契约与对账基线](issues/RT-01-shared-contract-baseline.md) — resolved；GPT 独立审计 PASS，Gemini 前端消费复核 `ACCEPT`，共享基线 `BUE-V1-RT01-20260824` 已冻结。
- [RT-02：调研物品拖动、坐标与 Glazier 渲染链](issues/RT-02-frontend-inventory-ui-coordinate-research.md) — `resolved`；Gemini 完成前端源码调研与三轮返修，GPT 最终证据源哈希复核 PASS；运行义务保留在 `VO-RT02-01`～`03`。
- [RT-03：调研统一设置 UI、模块状态与 Headless 隔离](issues/RT-03-frontend-settings-lifecycle-headless-research.md) — `resolved`；Gemini 完成设置/生命周期/Headless 调研与三轮返修，GPT 最终证据源哈希复核 PASS；运行义务保留在 `VO-RT03-01`～`03`。
- [RT-04：调研库存原生权威链与容器状态](issues/RT-04-backend-inventory-authority-container-research.md) — `resolved`；GPT 源码研究与第 2 轮独立审计 PASS，Gemini 对原生投影、`AwaitingProjection`、Storage generation 和契约充分性给出 `ACCEPT`。
- [RT-05：调研后端生命周期、LMN、设置与双端引用交集](issues/RT-05-backend-runtime-network-settings-research.md) — `resolved`；GPT 研究与 Gemini 复核 PASS，SourceSet 统一迁移完成；A/B 尖峰均通过第 2 轮独立审计，`SCR-RT05-001` 裁定 A 为 BUE V1 基线。
- [RT-06：联合收敛共享 seam 与实施就绪包](issues/RT-06-joint-seam-implementation-readiness.md) — `ready-for-human`；GPT 就绪包经四轮修复/独立审计后 `PASS`，仅等待 Gemini 最终消费复核。

上述任务只授权契约基线、U3-SDK 调研和实施就绪收敛，不授权生产功能编码。任务采用 `RT-` 前缀以避免与已完成的 GPT/Gemini Wayfinder 决策票混淆。

SourceSet：[`BUE-SS-20260824-02`](GPT-BUE-SS-20260824-02-Manifest.md) 已 `approved-frozen`；RT-02～RT-05 已全部迁移，Gemini 迁移复核 `PASS`。

当前 frontier：RT-01～RT-06 已关闭；DEV-01 已完成 GPT 独立审计与 Gemini `ACCEPT`。DEV-02 正在实施 Definition Linker、Compiled Catalog 与 Bootstrap 骨架。

Production frontier：RT-01～RT-06、DEV-01～DEV-06 已完成 GPT 独立审计与 Gemini 消费复核并关闭；DEV-07/08/09 仍保留各自真实环境运行门禁；DEV-10～DEV-12 已完成静态/单 DLL Host 验收，其中 DEV-12 已完成人工单 DLL 客户端 Bootstrap 冒烟。DEV-13「独立 No-op Feature 注册运行时与 Catalog Barrier」已完成构建、7/7 测试、GPT 独立审计 PASS，当前 `ready-for-human`，等待 Gemini 复核与 BUE + No-op 双 DLL 客户端冒烟。以上均不代表 ReleaseReady、Stable、整体可发布或三环境运行 PASS。

## Proposed architecture frontier

- [GPT-18：BepInEx 前置框架运行时与独立功能注册](issues/GPT-18-open-runtime-feature-framework.md) — `needs-triage`；后端独立复核为 `REVISE`。产品定位接受，但需先关闭 `SCR-GPT18-001`，冻结 BUE Host 物理部署、公开注册时序、ClientUi satellite、官方功能归属与 LoadSetIdentity，才能取代 GPT-06/GPT-14 的“V1 不动态加载外部功能 DLL”冻结决策。
- `SCR-GPT18-001` 已形成候选契约并通过 GPT 独立审计 R1，状态 `ready-for-human`；等待 Gemini/人工双端复核，尚未冻结共享 Contracts。

## Out of scope

- 独立 Web 前端、HTTP 服务和外部数据库。
- 首版自动交换或自动重排已占用物品。
- 用某一个运行环境的结果替代单人、SteamP2PFriends 或 U3DS 的独立验收。
- Wayfinder 阶段的生产代码实现与正式发布。
