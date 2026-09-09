# BUE Phase-3 规格：生态开发者平台能力定界与接线

Status: ready-for-agent
来源：`.scratch/bue-v2-phase3-platform/map.md`（completed）+ V3-T1..T10 决策票 Answer + V3-R1/R2 研究报告；本规格只把已裁决内容成文，不重开已关闭争议。

## 事实基线（历史规划与已建成事实的分界）

1. Phase-2 已将 BUE 自有网络运行时与 BUE 帧路径建成（DEV-V2-14..25，v8 正式交付，RELEASES 行 10）；原愿景文档中「LMN 底层实现→Adapter」演进节属历史规划，已被超越；
2. 愿景阶段 5「迁移现有插件」已由 Phase-2 完成（LIT/LIR/LHT 均为官方源码模块）；
3. 本规格以当前已验证的 BUE Network 实现和契约为准；任何未重新验证的历史规划不得自动视为当前实现；
4. 历史愿景存档（`.scratch/bue-v2-phase2-official-adoption/research/2026-09-07-bue-platform-vision-phase3.md`）原文逐字保留，不改写。

## Problem Statement

第三方 Unturned 插件作者想把功能接入 BUE 运行时时，面对的是一张「纸面可用、实证未穿」的接入面：公开注册桥虽然存在，但设置、日志、生命周期资源等平台服务注入为空，隔离逻辑分散在各组件的私有布尔标志里，事件路由存在可伪造路径，发送路径没有平台级防刷保底，诊断证据靠人工翻日志。作者无法从一份契约文档得知哪些面是稳定承诺、哪些是内部实现；一个功能故障可能波及其他功能；出了问题要靠手工复制日志、人工翻屏定位。

官方功能自己同样吃着这些未接线的缝——「官方先行消费」的契约面同权门禁因此无法满足。

## Solution

把八个平台缝按已冻结的裁决接线并契约化：注册桥加官方身份白名单与逐码拒绝红测；事件总线加类型归属一致性检查；宿主统一功能状态机接散装标志；网络加每会话发送预算与链路健康摘要；宿主时钟语义登记；设置注入接线与面板动态路由；诊断统一 Logger 与有界摘要。全部语义汇入 SDK 契约文档三个新附录，配合 NoOp 活样板与生态 DLL 上架自检清单，形成单一 SDK 契约面。

从使用者视角：生态作者读一份文档、对照一个活样板，经同一条注册路径接入全部平台服务，明确知道每个失败长什么样；玩家侧功能故障只隔离故障功能、面板可启停、证据可一键由 UMM 导出带走；官方功能与生态功能在同一套规则下运行。

## User Stories

1. As an 生态功能作者, I want 经 `BueRuntimeHost.Register` 注册时得到显式的接受或拒绝结果（含原因码与诊断码）, so that 我能在被拒时明确降级而不是猜测失败原因
2. As an 生态功能作者, I want 官方 FeatureId 保留段被确定性拒绝（`ReservedFeatureId`/`BUE-REG-010`）, so that 我不会误以为冒用官方身份可以成功
3. As an 生态功能作者, I want 注册拒绝码表在 SDK 文档中逐码登记, so that 我能按原因分支处理（等待/禁用自身/提示缺前置/报版本不兼容）
4. As an 生态功能作者, I want 注入的 `IFeatureBootstrap` 上 Network/Events/OwnedEvents/Identity/LifecycleGeneration 五成员生命周期内永非 null, so that 我不需要对平台服务做空值防御
5. As an 生态功能作者, I want Settings/Logger/Dependencies/Lifetime 四成员在本阶段恒 null 且文档明示不可用, so that 我不会依赖未承诺的面
6. As an 生态功能作者, I want 只能以自己的 FeatureId 派生事件身份发布功能事件, so that 事件协作不会出现身份冒用
7. As an 生态功能作者, I want 发布他人公开事件类型的载荷被归属检查拒绝, so that 我消费到的 TidyCompleted/HostTick 一定来自真实发布者
8. As an 生态功能作者, I want 订阅句柄幂等注销且停止时自动清理, so that 我不需要手写订阅生命周期管理
9. As an 生态功能作者, I want 查询自己功能的当前状态（状态/修订/隔离原因/代际）, so that 我能在被隔离时向用户给出有意义的提示
10. As an 生态功能作者, I want 经 `TryTrack` 登记资源并在停止时被逆序自动释放, so that 我不需要手写清理顺序
11. As an 生态功能作者, I want 被隔离后经面板手动重新启用获得新生命周期代际, so that 我能从干净状态重试而不背旧代际残留
12. As an 生态功能作者, I want 知道入站网络回调运行在传输泵线程, so that 我不会在其中误碰 Unity 对象
13. As an 生态功能作者, I want 把主线程工作投递给统一的平台 dispatcher, so that 我不需要自建线程泵和队列
14. As an 生态功能作者, I want 发送被平台限流时收到显式 `Throttled` 结果, so that 我能区分「平台节流」与「传输失败」并安全降级
15. As an 生态功能作者, I want 会话链路 degraded/recovered 以电平式诊断呈现, so that 我能定位「发送持续失败」是链路问题而非我的协议问题
16. As an 生态功能作者, I want 宿主时钟无独立 Hz 承诺、按 DeltaTime/序号自节流, so that 我按官方推荐模式实现低频逻辑而不期待调度协商
17. As an 生态功能作者, I want 经注入的 Settings view 读快照、提交变更并观察 revision 推进与校验拒绝, so that 我的设置与官方功能同纪律且不另造配置格式
18. As an 生态功能作者, I want 自己的设置在管理面板与官方设置同样可见可编辑, so that 玩家获得统一体验
19. As an 生态功能作者, I want 自己的诊断行进入同一 LogOutput 与诊断摘要（不被静默过滤）, so that 我的功能出问题时有与其他功能同权的证据链
20. As an 生态功能作者, I want 一份「上架前自检清单」, so that 发布前能人工核对引用方式、身份合规与降级义务
21. As a 玩家, I want 单个功能故障只隔离该功能、其他功能与原版继续运行, so that 一个插件的 bug 不毁掉整局游戏
22. As a 玩家, I want 在管理面板启用/停用任意已接入功能并看到状态更新, so that 我能按需裁剪体验
23. As a 玩家, I want 被 UMM 导出的日志里直接看到结构化诊断与摘要, so that 反馈问题时附一个文件就够
24. As a 官方功能维护者, I want 官方功能与生态功能走同一条注册/生命周期/服务获取路径, so that 契约面同权可被持续检验而非纸面声明
25. As a 官方功能维护者, I want 每个新公开契约面先由官方功能真实消费并通过测试, so that 契约不会停留在静态空壳
26. As a 仓库维护者, I want DEV-V3-01..08 不授候选身份、DEV-V3-09 出唯一 2.1 候选并一次加 RELEASES 行, so that 台账身份唯一、发布纪律不漂移

## Implementation Decisions

以下全部来自 V3-T1..T10 已裁决条目（细节以对应票 Answer 为准）。

### 共享规则（V3-T1）

- **契约面同权**：凡列入公开契约的能力，官方与生态经同一 interface、同一注册/生命周期规则、同一错误与隔离语义使用，官方无私有捷径；同权≠同能力。检验门禁=**官方先行消费**：每个新公开面至少一个官方功能真实消费并通过测试；NoOpFixture 只证明生态注册路径可运行，不替代消费证明。
- **契约面定义**：契约面=SDK 文档明确列举并登记的成员；`public`≠public contract，未登记 public 面不构成稳定承诺。
- **身份纪律**：BUE GUID/AssemblyName 冻结，文件名与部署路径非身份，FeatureId≠GUID；`io.github.yu80rice.bue.*` 为官方保留段，生态 FeatureId 用作者反向域名。
- **发布纪律**：决策票 plan-only；实施票候选经 红测→绿测→双轴 CLEAN→必要实机→SHA-256/CaseId→RELEASES 行。

### 注册桥与 Bootstrap（V3-T2 → DEV-V3-01）

- 官方身份白名单：保留段 FeatureId 须 ∈ 白名单（BII/LIT/LIR/LHT/BUE Network/ClientUi satellite 等），否则 `ReservedFeatureId`/`BUE-REG-010` 拒绝；判定顺序=基础校验→格式校验→保留段→合同版本→重复→结果；不反射 caller、不读路径。
- Admission 三类型（Result/Phase/Reason）与 `BUE-REG-001..010` 码表整体入冻结面；拒绝=显式结果不抛异常。
- Bootstrap 分层承诺：Identity/LifecycleGeneration/Events/OwnedEvents/Network 五成员永非 null（入冻结面+红测）；Settings/Logger/Dependencies/Lifetime 恒 null 为冻结语义（其中 Settings 归 06 票、Logger 归 07 票、Lifetime/Dependencies 归 03 票接线）。
- 不引入 registration session；`SupportedContractMajor=2`/`Minor=1`，2.0 模块继续可注册。

### 功能事件（V3-T3 → DEV-V3-02）

- 新路由不变量：**发布者 owner == 载荷类型归属 owner**（TidyCompleted→LIT、HostTick→宿主保留身份、生态事件→其 FeatureId）；未登记类型按明确规则拒绝，不静默放行； eventId 前缀校验保留。
- 拒绝=显式失败+结构化诊断+不调用任何订阅者；两接口（IFeatureEventSubscriber/IOwnedFeatureEventPublisher）语义入冻结面；FeatureEventBus 类本体保持内部自由。
- 不引入统一 envelope；事件总线=进程内本地（跨机走 BueNetworkApi）。

### 生命周期（V3-T4 → DEV-V3-03）

- `FeatureState`/`FeatureStatusView`/`StateRevision` 接线为唯一状态投影；散装布尔标志收编为内部实现；模块不可改状态。
- `IFeatureLifetime.TryTrack` 接线：停止后按注册逆序 Dispose、单 Dispose 异常隔离进诊断、容量必须有上限（数值留实施票）、已停止/隔离功能不可再登记。
- 只读状态查询缝（扩展 IFeatureLifetime 或独立只读 view 由实施票定，本规格冻语义不冻形状）；Dependencies=只读目录能力查询（Has/TryGet），不是求解器。
- 两代际轴分离（LifecycleGeneration vs ConnectionGeneration）；再启用=新代际旧代际全失效；Isolated 不自动重启；面板启停 seam 落地（面板=command adapter）。
- `CoreSafeMode` 只由组合期不变量损坏触发（catalog 冻结失败/核心 capability 组合失败等）；运行期单功能失败永不升级，只走功能级隔离。

### 网络（V3-T5 → DEV-V3-04）

- 发送预算：平台按会话保底限流，超出=新 `NetworkSendResult.Throttled`（加性枚举值，旧模块须对未知结果安全降级）；预算随 ConnectionGeneration 隔离；官方生态一视同仁；不静默丢弃。窗口/令牌数/容量留实施票。
- 业务重试与退避不上收（LIT 挑战重臂保留功能私有）；LIT 告警限频被链路健康接管后退役。
- 入站回调线程=传输泵线程（SDK 冻结登记）；提供统一主线程 dispatcher 服务（容量上限/超限显式失败/任务绑定模块代际/停止失效/单任务异常隔离/禁自建泵）；LIR 迁移为官方先行消费者。
- 会话链路健康：连续失败达阈值（默认 10，先例）→一次 degraded 诊断；成功恢复→一次 recovered+清零；每会话代际独立。
- 入站 handler 异常从静默吞改为结构化诊断（不扩散、不打穿泵线程）；不新增网络状态查询面。

### 宿主时钟（V3-T6 → DEV-V3-05）

- 零新增契约面、不触发版本变化；八条语义登记（保留身份防伪造/每拍恰一 tick 去重归宿主/Phase=Update=0/序号从 1 严格单调/DeltaTime 回拨钳零/载荷只含时序三字段/异常不扩散进功能级隔离/主线程构造性保证）。
- 自节流=官方推荐模式（LHT 先例）；派生低频时钟挂需求信号雾区。

### 设置（V3-T7 → DEV-V3-06）

- `bootstrap.Settings` 接线（恒 null→可用）：view 限当前功能作用域（GetSnapshot/TryGet/Submit）；官方与生态共用 SettingsRuntime 规则（校验/revision 单调/损坏安全默认/原子提交/作用域隔离）；类本体不列契约。
- 面板按注册目录动态路由（官方硬编码清单退役）；面板=编辑 adapter 非第二事实源；未提供设置的功能不伪造设置页。
- ClientPreference/ServerAuthority 双 scope：U3DS 与 P2P 主机权威端同语义；会话覆盖断线清除不污染持久化 revision；不做跨机同步协议。
- schemaVersion 通道保留，迁移由功能自理；`ExpectedRevision` 防旧 UI 覆盖新值。

### 诊断（V3-T8 → DEV-V3-07）

- `IFeatureLogger` 接线（恒 null→可用）：每模块绑定自身 FeatureId 的 view，三方法窄面（Info/Warning/Error）；Logger 异常不得反向破坏模块。
- 有界诊断摘要：按 DiagnosticId 聚合（FeatureId/级别/计数/首末时间），容量受限/输出限频/重启不持久；结构化行写入 BepInEx LogOutput。
- **BUE 不建日志复制器/采集器/导出动作**——原始日志导出归 UMM 人工流程（Player.log+LogOutput.log）；摘要≠验收授权。
- `BUE-*` 平台诊断前缀保留；生态诊断码用 FeatureId 派生前缀，冒用=拒绝写入+诊断。
- T4 隔离/T5 链路健康/状态投影进统一诊断 sink。

### SDK 契约文档（V3-T9 → DEV-V3-08）

- 正文八节冻结不动；新增附录 A（平台服务参考：Admission/Events/Lifecycle/Network/HostTick/Settings/Diagnostics 七节）、附录 B（诊断与身份码表：BUE-REG-001..010、BUE-PLATFORM-001/002、前缀纪律、FeatureId 保留段及合法/非法示例）、附录 C（契约版本与迁移：2.0→2.1 条目、安全降级原则、Major 纪律、四条件门禁、RELEASES 注记要求）。
- Contracts 拆分四条件全部未触发→继续暂缓，逐条登记为门禁条款（定义/事实判定/触发信号/重评义务；先触发预判=①编译脱耦、③发布节奏分化）。
- NoOpFixture=统一生态契约 probe（注册→Bootstrap→Events→TryTrack/Lifecycle→Network→HostTick→Settings→Logger→停止与隔离全链）+「生态 DLL 上架前自检清单」（11 项人工核对）。
- SDK 文档随主 DLL 契约版本走，不独立发版；文档示例锚定 NoOpFixture 真实代码。

### 契约版本

T2..T8 的 Minor 加性变更单一批次合入：**2.1**（若实际分批则顺延 2.2——分批是允许的实施计划，不是禁止项）；宿主门槛 Major=2 不动。

## Testing Decisions

- **红测先行**：每张实施票先立红测锚点再实现；红绿后跑双轴独立审查（standards-reviewer/Spec-Reviewer 专属智能体）→ CLEAN 才交付（docs/agents/output-review-loop.md）。
- **只测外显行为**：经公开 seam 断言结果（结果四元组/状态投影/结构化诊断行/摘要计数），不断言内部锁、表结构或私有标志。
- **测试缝与既有先例**（全部既有缝，零新测试缝）：
  1. 注册桥：显式结果断言（Accepted/FeatureId/Reason/DiagnosticId），逐码锚 001..010；先例=注册运行时既有宿主测试。
  2. 事件总线：归属拒绝/前缀拒绝/句柄幂等/异常隔离/UnsubscribeAll 完整性；先例=DEV-V2-19 总线测试组。
  3. 生命周期：假模块驱动状态机（Start 抛异常→Isolated 不扩散；TryTrack 逆序 Dispose；UserDisabled 停/新代际启）；先例=NoOp+假模块宿主测试。
  4. 网络：LocalLoopbackTransport 假传输驱动（Throttled 首条/连续超限/恢复/跨代际清零；链路健康电平；入站异常诊断）；先例=LocalLoopbackTransport+DEV-V2-16 发送结果测试组。
  5. 宿主时钟：fake-clock 组（序号单调/钳零/暂停恢复语义）；先例=DEV-V2-19 fake-clock 钉死组。
  6. 设置：InMemorySettingsPersistence（快照/提交/校验失败/ExpectedRevision 过期/损坏安全默认）；先例=Settings 测试工程既有组。
  7. 诊断：sink 捕获（Logger 行→LogOutput 结构化行→摘要计数；BUE-* 前缀拒绝；容量受限降级）；先例=DiagnosticLogSink/Recorder seam 与 `--*-red` 锚点。
  8. NoOpFixture 契约 probe=生态侧总集成缝，覆盖 T3/T4/T5/T7/T8 各票扩链清单；U3DS/实机行为走 DEV-V3-09 三环境验收，绑定 LoadSetIdentity 轻量链。
- **官方先行消费锚**：每缝至少一个官方功能消费断言（LIR 迁移 dispatcher、官方功能走注入 Settings view 与 Logger view、官方身份白名单正例）。
- **测试工程归属**：契约断言走 Contracts.Tests；运行时行为走 Plugin/Network.Tests 等既有七个测试工程直跑口径，不新建测试工程。

## Out of Scope

**后续阶段候选（需求信号驱动，本规格不实现）**：可靠通道分档；认证与加密；跨服中继；跨机设置同步协议；平台统一迁移框架；诊断附件 API；派生低频时钟；per-feature 调度参数；BueThreading 后台任务（取消/队列上限/背压）；SDK 项目模板；编译期验证工具；UMM 诊断包自动化；诊断实时视图；健康阈值用户可配置化。

**Phase-3 不做的愿景模块**：BuePatching（保留 T4 隔离兜底+现有各模块自有补丁+SDK 补丁边界说明）、BueCompatibility（现有契约门槛+只读查询+环境角色判断已够）、BueUi（面板动态路由已属 Settings 的 UI adapter）——三者均不预建空接口。

**产品边界永久冻结**：自建 DLL scanner；自建外部 loader；重复实现 BepInEx 发现/排序；第二套依赖求解器；自动重启 Isolated 功能；未满足四条件即拆 Contracts.dll；把 UMM 原始日志导出职责搬入 BUE；让模块自行修改宿主状态。重定义产品边界须另开架构 effort。

**其他**：不改历史愿景存档原文；不做入站回调改主线程；不做可靠等级平台化（LHT 双可靠度仍功能语义）；不重开 T1..T9 已关闭争议。

## Further Notes

- **DEV-V3 实施票计划（V3-T10 冻结）**：一票一 seam 共 9 张——01 注册桥/Bootstrap、02 FeatureEventBus、03 BueLifecycle、04 BueNetwork、05 HostTick、06 BueSettings、07 BueDiagnostics、08 SDK 附录总装+四条件落档、09 三环境验收+RELEASES+publish 换新。真实依赖：01→02/03；01+03→04；03→05/06；01+03+04→07；01..07→08→09。不按票号强行并行。
- **候选策略**：01..08 各自红绿+双轴 CLEAN 即提交，**不产正式候选 DLL、不更新 RELEASES、不授 CaseId**（中间诊断构建≠候选≠发布物≠可继承证据）；09 生成唯一 Phase-3 `2.1` 候选，三环境验收+SHA-256/CaseId 绑定+人工批准后加 RELEASES 行+publish 交付包同步换新。
- **移交总账防遗漏**：T9 Answer 的 T1..T8→SDK 条目总账是附录 A/B/C 的填充目录，实施期每条必须映射到具体章节；DEV-V3-08 结票前逐条核对。
- **契约版本注记**：「单一批次 2.1」是当前实施计划而非永久规则；实际分批发布时按 Minor 顺延即可。
- **下一步**：`/to-tickets` 按 Further Notes 的票计划与依赖边产出 DEV-V3-01..09 实施票（issues/ 目录，ready-for-agent），每票独立会话 `/implement`。
