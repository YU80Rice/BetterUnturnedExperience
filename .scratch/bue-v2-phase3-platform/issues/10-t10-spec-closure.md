# V3-T10 Phase-3 规格闭包与实施票拆分

- **Ticket**: V3-T10
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-10 五项裁决定音，地图到达 /to-spec 出口）
- **Blocked By**: V3-T2, V3-T3, V3-T4, V3-T5, V3-T6, V3-T7, V3-T8, V3-T9
- **Map**: [map.md](../map.md)

## Question

规格闭包票：全部决策票 resolved 后执行，产出可交 `/to-spec` 的完整规格输入。

1. **范围扫拢**：阶梯四级+诊断的逐项裁决（本阶段实现 / 后续阶段 / 永久不做）汇总成无歧义的范围表；
2. **雾区裁定**：三个未入阶梯的愿景模块（BuePatching / BueCompatibility / BueUi）去留对照裁定（见 map Not yet specified），届时雾 graduate 成票或落 Out of scope；
3. **愿景文档改写指令**：「LMN 底层实现→Adapter」演进节按已建成事实（BUE 帧运行时）改写、阶段 5 标注已由第二阶段完成——形成 /to-spec 的输入清单；
4. **实施票拆分规则**：DEV-V3-* 的拆票粒度、依赖边、候选与 RELEASES 节奏、红测+双轴门禁的套用点；
5. **spec 出口自检**：对照 Destination 四条到达条件逐条核验，全过才交 /to-spec。

## Answer

2026-09-10 用户+PM 五项裁决全部定音，**Phase-3 地图到达 /to-spec 出口条件**。终点表述：形成一份范围闭合、契约闭合、证据闭合、实施拆分可执行的生态开发者平台规格输入，实现工作移交 DEV-V3 实施票——T10 结束不是「平台全部实现完成」，而是：方向已定→规格可写→实施可拆→依赖清楚→发布纪律不漂移。本票不重新打开 T1..T9 已关闭的架构争议。

### 1. 范围扫拢总表（定稿）

**本阶段实现（DEV-V3）**：①注册桥=官方保留段白名单+`ReservedFeatureId`+Admission 逐码红测+Bootstrap 五成员永非 null/四成员恒 null；②事件=类型归属登记+发布者与类型 owner 一致性+两公开接口语义登记+锁外派发/异常隔离/停止注销；③Lifecycle=`FeatureState`/`FeatureStatusView`/`StateRevision` 接线+`TryTrack` 注册与逆序释放+只读状态查询+`UserDisabled` 面板启停 seam+`CoreSafeMode` 组合期触发+功能级隔离；④Network=每会话发送保底限流+`Throttled`+链路健康 degraded/recovered+入站异常结构化诊断+主线程 dispatcher+LIR 官方先行消费+业务退避仍由功能持有；⑤HostTick=现行语义 SDK 登记+红测补齐+NoOp probe+不新增调度参数或派生时钟；⑥Settings=`bootstrap.Settings` 注入+面板按注册目录动态路由+双 scope+revision/schemaVersion/会话覆盖语义；⑦Diagnostics=`IFeatureLogger` 接线+有界 DiagnosticId 摘要+`BUE-*` 前缀保留+结构化行写 BepInEx LogOutput+UMM 继续人工导出；⑧SDK=正文八节冻结+附录 A/B/C+四条件拆分门禁+生态 DLL 上架前人工自检清单+NoOp 活样板锚点+T1..T8 移交总账。契约演进统一 **Minor→2.1 或按实际发布批次顺延 2.2**；「单一批次 Minor」只是当前实施计划，不得写成禁止分批发布的永久规则。

**后续阶段（需求信号驱动候选）**：可靠通道分档、认证加密、跨服中继、跨机设置同步、平台统一迁移框架、诊断附件、派生低频时钟、per-feature 调度参数、BueThreading 后台任务、SDK 模板、编译期验证工具、UMM 诊断包自动化、诊断实时视图、健康阈值用户配置。

**永久不做（当前产品边界冻结）**：自建 DLL scanner、自建外部 loader、重复实现 BepInEx 发现/排序、第二套依赖求解器、自动重启 Isolated、未满足四条件即拆 Contracts.dll、把 UMM 原始日志导出职责搬入 BUE、让模块自行修改宿主状态。若未来产品目标被正式重定义，必须另开架构 effort，不得在本图内悄悄解除。

### 2. BuePatching / BueCompatibility / BueUi = Phase 3 不做，后续阶段候选

BuePatching：本阶段只保留 T4 宿主回调异常隔离+现有 Harmony implementation+各模块自有补丁+SDK 补丁使用边界说明；不建统一 Patch registry/Transformer/IL 兼容平台。BueCompatibility：契约版本门槛+IDependencyCapabilityView 只读查询+环境角色判断+BepInEx 前置声明已够当前目标；不建兼容性数据库/自动矩阵工具/第二套版本求解器。BueUi：T7 面板动态路由属 Settings 的 UI adapter，不代表独立 BueUi 模块；不建通用 UI SDK/UI 组件库/主题系统/远程管理界面/生态 UI 注册平台。三者统一登记「Phase 3 不做，后续阶段候选」；**不写成「当前已有完整模块」，不为它们预建空接口**。

### 3. 愿景文档 = 历史存档原文不改

原始对账注记逐字不变，保留历史时点与决策演进。新事实只写入 /to-spec 生成的 Phase-3 规格「事实基线」节：①Phase-2 已将 BUE 自有网络运行时和 BUE 帧路径建成；②原愿景「LMN→Adapter」段落属历史规划；③Phase-3 spec 以当前已验证的 BUE Network 实现和契约为准；④未重新验证的历史规划不得自动视为当前实现。需附：Phase-2 票据、v8 发布物与证据、当前源码路径、旧文档原文位置。不直接改写历史段落——保护「当时写了什么」与「后来建成什么」的可追溯性。

### 4. DEV-V3 实施票拆分 = 一票一 seam，按真实依赖执行（不强行并行）

票结构：DEV-V3-01 注册桥/Bootstrap、02 FeatureEventBus、03 BueLifecycle、04 BueNetwork、05 HostTick、06 BueSettings、07 BueDiagnostics、08 SDK 附录总装+四条件落档、09 三环境验收+RELEASES+publish 换新。每票负责：该 seam 接线+红测+NoOp probe 扩链+SDK 对应附录落位+双轴独立审查。

依赖关系（真实契约依赖，非票号整齐）：

```text
DEV-V3-01 → 02 / 03
DEV-V3-01 + 03 → 04
03 → 05
03 → 06
01 + 03 + 04 → 07
01..07 → 08 → 09
```

**候选策略=单候选批量交付+「中间 DLL 不授身份」**：DEV-V3-01..08 各自红绿+双轴 CLEAN，**不产正式候选 DLL、不更新 RELEASES、不授 CaseId**，只留测试/审查/增量证据；DEV-V3-09 基于全部合入后的源码生成唯一 Phase-3 `2.1` 候选，三环境验收+完整 SHA-256/CaseId 绑定+人工批准后更新 RELEASES+更新 publish 交付包。中途需实机诊断可生成临时构建，但诊断构建≠候选≠RELEASES 发布物≠可继承运行证据。

### 5. 结票后交付动作（已执行）

①关闭地图（四项闭包条件全过：阶梯四级逐项裁决完成/关键 interface·seam·失败语义有票面结论/本阶段·后续·永久不做边界明确/DEV-V3 拆票与 /to-spec 输入完整）；②归档 `.scratch/bue-v2-phase3-platform/`（map+10 决策票+2 research 票+research 报告）入库；③CONTEXT.md 本轮已随各票即时入典（契约面同权/官方先行消费/功能状态投影/生命周期代际/事件类型归属/发送预算/主线程投递/设置作用域/诊断摘要/官方 FeatureId 保留段+多处锐化），无重复改写——只写未来探索者需要的领域事实；④fresh 会话执行 /to-spec：输入=Phase-3 map+V3-T1..T10 Answer+V3-R1/R2 报告+CONTEXT.md+SDK/Phase-2 证据；/to-spec 只把已裁决内容写成完整规格，不重新发起 T1..T9 已关闭的问题（自建加载器争议/Contracts 拆分争议/SDK 模板争议/RELEASES 候选策略争议均已关闭）。

### 本票不做

不修改历史愿景原文；不建设 BuePatching/BueCompatibility/BueUi；不创建新 scanner/loader；不把 DEV-V3 票强行并行；不为中间实现授正式候选身份；不自动修改 RELEASES；不重新讨论 T1..T9。

## Comments
