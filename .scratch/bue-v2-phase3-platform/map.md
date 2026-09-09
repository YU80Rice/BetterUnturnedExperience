# V2 第三阶段：生态开发者平台能力定界与规格冻结（Wayfinder 地图）

Type: task
Status: **completed（2026-09-10：10 决策票+2 research 票全部 resolved；Destination 四条到达条件自检全过；地图关闭，下一步 = fresh 会话 `/to-spec`（输入=本图+12 张票 Answer+R1/R2 报告），实施移交 DEV-V3-01..09）**
Label: wayfinder:map
Parent: 无（承接 [[bue-v2-phase2-wayfinder]]：V2 第二阶段 14..25 全闭环，v8 已正式交付）
Author: GPT（本会话 charting）

## Destination

BUE Phase 3：生态开发者平台能力定界与规格冻结。

到达条件：
1. BueNetwork、BueLifecycle、BueSettings/BueEvents、生态 SDK 的本阶段范围逐项裁决；
2. 公开注册桥、平台服务、模块状态和生态 DLL 接入路径的 interface / seam / 失败语义明确；
3. 每项明确本阶段实现、后续阶段或永久不做；
4. 形成无关键歧义、可交 /to-spec 的完整规格输入。

## Notes

- 领域：Unturned（U3DS 2022.3.62）+ BepInEx 5.4.23.5；BUE v8 正式交付基线（RELEASES 行 10，SHA-256 `f7b7513c…df569`）；本图 plan-only，生产实施随 /to-spec→/to-tickets 产生。
- **词汇（本图定音，2026-09-09 用户裁决）**：**阶梯四级** = 愿景 ladder 四级（BueNetwork 收敛 / BueLifecycle / BueSettings+BueEvents / 生态 SDK），地图主对象；**对账残差四项** = 限流入平台 / 主线程投递契约化 / 诊断包自动化 / Contracts 拆分重评（愿景对账注记差值），作为输入摊入 T5/T5/T8/T9 作具名子问题——残差四项不平级叙述、不设总清单票。
- **阶梯决策票（T2..T9）统一六问结构**：本阶段是否实施 / 实施深度 / 明确留到后续的内容 / 需要的公开 interface、seam、测试与验收门禁 / 是否影响已有 BUE 契约 / 是否影响官方-生态同权模型。
- **风险分级（排序记录用）**：**Strong** = T2 注册桥+Bootstrap composition、T3 EventId 路由、T4 Lifecycle 状态/租约；**Worth exploring** = T5/T6/T7/T8/T9；**Speculative** = Contracts.dll 拆分、自建 scanner/loader。
- **图级冻结（2026-09-09 用户裁决）**：不建设 BUE 自有 DLL scanner；不建设自有外部 loader；不把 BepInEx 的发现/排序职责搬进 BUE；不因 FeatureDependency 类型存在就建设第二套依赖求解器；本图不直接拆分 Contracts.dll（T9 只重评四条件门禁）；不为「未来可能有用」提前扩张公开 Major 契约。
- **已裁决事项（不立票，2026-09-09）**：目录 `.scratch/bue-v2-phase3-platform/`；决策票 V3-T1..T10、research 票 V3-R1/R2（票头统一 Ticket/Type/Status/Blocked By）；实施票由 /to-tickets 产生 DEV-V3-*（CaseId 对应 RELEASES）；/to-spec 出口 = V3-T10 resolved；旧「目录票号」票已折叠入本 Notes；T2 合并注册桥/Registration Admission/Bootstrap composition 一票；决策票走 grilling + domain-modeling，research 票 AFK 子代理（resolve 后由主会话回写本图指针）。
- **已建成事实（各票对照基线，不重开）**：公开注册桥 `BueRuntimeHost.Register` 在位（SCR-GPT18-001）；NoOpFixture 为生态路径活样板；BUE 帧运行时已自建——愿景文档「LMN 底层实现→Adapter」演进节已被超越、阶段 5「迁移现有插件」已由第二阶段完成；/to-spec 时按已建成事实改写愿景文档该两节。
- **实施期硬规则（届时生效）**：红测先行 + 双轴独立审查（standards-reviewer / Spec-Reviewer 专属）→ CLEAN 才交付（docs/agents/output-review-loop.md）；大写入分批（docs/agents/large-write-batching.md）。
- **主源锚点**：愿景定稿 `.scratch/bue-v2-phase2-official-adoption/research/2026-09-07-bue-platform-vision-phase3.md`（对账注记 1–3）；T7 四条件 `.scratch/bue-v2-phase2-official-adoption/issues/07-developer-contract-double-install.md`；开发者契约唯一事实源 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`；契约面 `src/BetterUnturnedExperience.Contracts/ContractTypes.cs`；词汇典 CONTEXT.md。

## Decisions so far

- [V3-R2 诊断包自动化现状研究](issues/12-r2-diagnostics-automation.md)：现状=三层全人工——无 BueDiagnostics、无生产打包代码，自动化止于结构化日志（BueRuntimeLog+internal DiagnosticLogSink）与 U3DS SOP 的 `cp LogOutput.log`；SP/P2P 主链靠用户 UMM 导出+事后拷 `audit/`（24 的 auto-evidence/ 为空）；CaseId/RELEASES 登记始终人工。分层事实：层①（一键采集）有 UMM/SOP 对照但未接 git、层②（会话内自动留档）源码与 SOP 均无、层③（采集器 API）仅未接线的 IFeatureLogger（生产传 null）；三层都不自动授予 CaseId。决策入 V3-T8（现已解锁）。报告 `research/2026-09-09-V3-R2-diagnostics-automation.md`。

- [V3-R1 平台公开面现状盘点](issues/11-r1-platform-surface-inventory.md)：注册桥 `BueRuntimeHost.Register` 已实现且官方/NoOp 同一入口（DEV-V2-24 改名实机 accepted=True），但仓库外第三方 DLL 经桥注册证据未找到；生产 `IFeatureBootstrap` 只接线 Network/Events/OwnedEvents/LifecycleGeneration，Settings/Logger/Dependencies/Lifetime 传 null，`IFeatureLogger` 无实现类；TidyCompleted/HostTick 契约与总线已实现有宿主测试，生态独立 DLL 实机消费未找到（NoOp 的 Start 返回 Started=false 不练这些缝）；设置与诊断走官方私有路径、面板路由写死官方 FeatureId，BUE-PLATFORM-001 红测六例+C4'' 实机双证；`EnterCoreSafeMode` 仅定义+注册拒绝逻辑、生产未调用，功能级隔离=分散 isolated 标志非统一 FeatureState 机；Contracts 嵌入主 DLL，T7 四条件均无触发实例，NoOpFixture 外无真实第三方需求信号。报告（含六态全表）`research/2026-09-09-V3-R1-platform-surface-inventory.md`。
- [V3-T1 跨阶梯共享裁决](issues/01-t1-shared-rulings.md)：六项定音——①**契约面同权+官方先行消费门禁**（同权≠同能力；现状不追溯，私有路径上收归 T2..T9）；②发现承诺纯重申（BepInEx 原生，BUE 不建 scanner/loader）；③**契约面=SDK 文档登记列举、public≠契约**，Major/Minor/Patch 规则+各票「契约影响」六问；④身份冻结重申+`io.github.yu80rice.bue.*` 官方保留段+生态反向域名指引（SDK 文档修订随 /to-spec）；⑤**决策票 plan-only**（不产候选/不加 RELEASES 行），实施票沿用现行节奏，证据链治理归 T8；⑥两层交付模型重申。T2..T7/T9 随本票 resolved 全部解锁。
- [V3-T2 公开注册桥、Registration Admission 与 Bootstrap composition](issues/02-t2-registration-bridge.md)：方案 A 现状加固七项定音——①保留段拒绝=**官方身份白名单**（`io.github.yu80rice.bue.*` ∉ 白名单→`ReservedFeatureId`/`BUE-REG-010`；不反射 caller、不读路径；判定顺序①基础②格式③保留段④合同版本⑤重复⑥结果）；②Admission 三类型+`BUE-REG-001..010` 码表整体入 SDK 冻结面（拒绝显式结果不抛异常）；③Bootstrap 三层承诺：五成员（Identity/LifecycleGeneration/Events/OwnedEvents/Network）永非 null 入冻结面+红测，四成员（Settings/Logger/Dependencies/Lifetime）**恒 null=冻结语义**登记不可用（注入归 T4/T7/T8）；④逐码红测锚 001..010；⑤契约 **Minor 2.0→2.1**（宿主门槛 Major=2/Minor=1，2.0 模块继续可注册）；⑥同权四条检验（同入口/保留段双向红测/无内部绕行/白名单不豁免其他 admission 规则）。不引入 registration session、不建依赖求解器、不拆 Contracts.dll。
- [V3-T3 FeatureEventBus 事件身份与授权路由](issues/03-t3-event-bus.md)：方案 A 七项定音——①新路由不变量 **`PublishedEventType.Owner == PublisherFeatureId`**（类型归属登记：TidyCompleted→LIT、HostTick→宿主保留身份、生态事件→其 FeatureId；「合法前缀+冒用载荷类型」伪造路径根治；未登记类型明确规则不静默放行；拒绝=显式+诊断+不调订阅者；定性=真实性检查非 ACL）；②两接口（IFeatureEventSubscriber/IOwnedFeatureEventPublisher）+完整语义入 SDK 冻结面，FeatureEventBus 类本体保持内部自由；③不引入统一 envelope，生态载荷只登记建议约定不运行时校验；④事件总线=进程内本地（不跨机/不持久化/不重放，跨机走 BueNetworkApi，文档给对照）；⑤红测三组（归属检查/既有语义/停止清理）；⑥加性 Minor（与 T2 同批共用 2.1 或顺延 2.2）；⑦同权四条+**NoOp 升级为生态契约 probe**（订阅→回调→停止注销→异常隔离全链）。
- [V3-T4 BueLifecycle 模块状态、代际与资源清理](issues/04-t4-lifecycle.md)：方案 A 七项定音（Strong 三连清）——①**FeatureState/FeatureStatusView/StateRevision=唯一状态投影**（宿主拥有九态机，散装布尔标志收编为内部实现，单模块状态变化不扩散）；②TryTrack 资源注册表接线（Stop 返回后注册逆序 Dispose、Dispose 异常隔离、容量必须有上限可观察可测试，兑现 T2 恒 null→可用授权）；③只读状态查询缝（禁 Setter/伪造修订/直调 StopAll）+Dependencies=只读目录能力查询（非求解器）；④**两代际轴分离**（LifecycleGeneration vs ConnectionGeneration）+再启用=新代际旧代际全失效+**UserDisabled 面板启停 seam 本票落地**（面板=command adapter）；⑤**CoreSafeMode 只处理核心组合期不变量**（运行期单功能失败永不升级，只走局部隔离；Harmony 触面归 BuePatching/T10）；⑥Minor 加性（十项冻结，2.1 共用或 2.2 顺延）；⑦同权四条+NoOp probe 扩链（TryTrack→逆序 Dispose→UserDisabled→新代际→状态/隔离可观察）+官方 dogfooding 走真 UserDisabled。
- [V3-T5 BueNetwork 绑定、限流、主线程投递与失败语义](issues/05-t5-network.md)：方案 B×2 八项定音（具名子问题 A/B 双落）——①限流**切分上收**：平台=每会话发送预算（超出→新 `NetworkSendResult.Throttled`，按 ConnectionGeneration 隔离，官方生态一视同仁，不静默丢弃），功能=业务重试/退避（LIT 重臂 {0,1s,2s,4s,8s} 不上收），LIT 告警限频被健康诊断接管后退役；②主线程投递：**入站回调=传输泵线程（SDK 冻结登记）+BUE 统一主线程 dispatcher 上收**（容量上限/绑定模块代际/停止失效/禁自建泵，LIR 迁移=dogfooding）；③失败可见化：会话级链路健康 Healthy/Degraded 电平式诊断（阈值 10 先例、可配置化留 Settings 票）+**入站 handler 异常静默吞补结构化诊断**；④不加网络绑定状态查询面（SendResult+Sessions 够用防浅 interface）；⑤现状加固五件套；⑥留后续：可靠分档（LHT 双可靠度仍功能语义）/认证/跨服/上游断线根治；⑦Minor 加性（Throttled+dispatcher seam+线程语义，2.1 共用或 2.2 顺延；旧模块对未知结果须安全降级）；⑧同权四条（官方同受限流红测/LIR dispatcher 先行消费/NoOp probe 扩链/官方存量消费者证据）。
- [V3-T6 宿主 Tick 调度 seam](issues/06-t6-host-tick.md)：方案 A 六项定音（全图最薄票）——①**统一时钟+功能侧自节流**（频率=宿主泵拍频无独立 Hz 承诺；不做频率协商/相位错开/派生时钟；LHT 10Hz 自节流=官方推荐模式）；②八条契约登记（宿主保留身份防伪造/每拍恰一 tick 去重归宿主/Phase=Update=0/序号从 1 严格单调/DeltaTime 回拨钳零+暂停语义自洽/载荷只含时序三字段/单订阅者异常不扩散进 T4 隔离/主线程构造性保证）；③实施=登记+红测补齐+NoOp probe 补 HostTick 支线；④**零新增契约面、不触发版本变化**（实施中要扩 Phase/载荷=至少 Minor 另议）；⑤派生低频时钟登记进地图雾区挂真实需求信号；⑥同权：官方无特殊频率、生态不被静默过滤。
- [V3-T7 BueSettings 与配置权威/迁移范围](issues/07-t7-settings.md)：七项定音——①官方与生态**共用同一 SettingsRuntime 规则**（快照/校验/revision 单调/持久化/损坏安全默认/原子提交/作用域隔离；路径 `<FeatureId>.bue-settings` 仅 adapter 约定，契约依赖 Settings interface 不硬编码路径）；②**bootstrap.Settings 正式接线**（T2 预授权兑现；view 限当前功能作用域）+SettingsRuntime/ISettingsPersistence 类本体保持内部自由；③**面板按注册目录动态路由**（收编官方硬编码清单，面板=编辑 adapter 非第二事实源）；④ServerAuthority/ClientPreference 双 scope 语义（U3DS 与 P2P 主机权威端同语义；会话覆盖断线清除不污染持久化；不做跨机同步协议）；⑤schemaVersion 通道+迁移功能自理（不建统一迁移 DSL）；⑥Minor 加性（Settings 注入+语义登记，2.1 共用或 2.2 顺延）；⑦同权四条+NoOp probe 扩链（读→提交→拒绝→停止失效）+官方 dogfooding 走真注入 view。
- [V3-T8 BueDiagnostics 日志、摘要与人工导出协作](issues/08-t8-diagnostics.md)：范围收窄版八项定音——**BUE 不建日志复制器/诊断包打包器/面板导出动作**（LogOutput.log=BepInEx 统一产生，UMM 人工导出 Player.log+LogOutput.log）；BUE 交付=①IFeatureLogger 接线（恒 null→可用，T2 预授权兑现，每模块绑定 FeatureId 的 view）+②有界诊断摘要（按 DiagnosticId 聚合 FeatureId/Level/Count/FirstSeen/LastSeen，容量受限/输出限频/重启不持久，结构化行写 LogOutput）+③T4 隔离/T5 链路健康/状态投影进统一诊断 sink+④**`BUE-*` 平台诊断前缀保留**（生态用 FeatureId 派生前缀，冒用=拒绝写入+诊断）。同权=同一日志出口与同一摘要机制（官方先行消费+NoOp probe 扩链）；采集≠验收，CaseId/RELEASES 仍人工。不做：附件 API/自动压缩上传/实时诊断面板/把 UMM 导出职责搬进 BUE。
- [V3-T9 生态 SDK、契约文档与 Contracts 拆分重评](issues/09-t9-sdk-contracts-split.md)：方案 A 七项定音（阶梯四级全清）——①SDK=文档+NoOp 活样板+已验证接入路径（**不建模板/验证 CLI/独立 SDK 程序集/第二样板**，挂雾区）；②具名子问题：**Contracts 拆分四条件全部未触发→继续暂缓**，正式登记为 SDK 门禁条款（逐条含定义/事实判定/触发信号/重评义务；先触发预判=①编译脱耦③发布节奏分化）；③正文八节冻结+**新增附录 A（平台服务参考七节）/B（诊断与身份码表）/C（契约版本与迁移+四条件）**；④NoOp=统一生态契约 probe（全缝链）+新增「生态 DLL 上架前自检清单」（人工核对 11 项）；⑤T1..T8 契约条目**正式移交总账**（T10//to-spec/实施票三用，每条映射 SDK 章节）；⑥本票零契约版本变化，SDK 文档随主 DLL 版本走不独立发版；⑦文档示例锚定 NoOpFixture 双向锚定+v8 包 SDK 文档=2.0 基线随 DEV-V3 发布同步换新。
- [V3-T10 Phase-3 规格闭包与实施票拆分](issues/10-t10-spec-closure.md)：五项定音（地图关闭）——①范围扫拢总表定稿（本阶段实现八缝/后续阶段需求信号驱动候选 14 项/永久不做 8 项——「单一批次 Minor」是实施计划非永久规则；产品边界冻结项重定义须另开 effort）；②**BuePatching/BueCompatibility/BueUi 均不入 Phase-3**（后续候选；不预建空接口）；③历史愿景存档原文不改，当前事实写新 spec「事实基线」节（附 Phase-2 票据+v8 证据+源码路径）；④**DEV-V3 一票一 seam 共 9 张**（01 注册桥→02 事件/03 Lifecycle→04 Network（需 01+03）→05/06（需 03）→07（需 01+03+04）→08 SDK 总装→09 验收），**01..08 不授候选身份，09 生成唯一 2.1 候选**（诊断构建≠候选≠RELEASES≠可继承证据）；⑤fresh 会话 /to-spec（输入=map+12 票 Answer+R1/R2+CONTEXT+SDK/Phase-2 证据；不重开已关闭争议）。

## Not yet specified

- **三个未入阶梯的愿景模块**（BuePatching 统一 Harmony 注册面 / BueCompatibility 版本能力判断 / BueUi）：去留在 V3-T10 规格闭包时按阶梯四级+诊断的裁决结果对照裁定（本阶段 / 后续阶段 / 另立 effort），届时雾 graduate 成票或落 Out of scope。
- **实施拆票节奏**：V3-T10 resolved 后 DEV-V3-* 的分批粒度、依赖边、候选与 RELEASES 节奏——/to-tickets 阶段定，不入本图。
- **派生低频时钟 / per-feature 调度参数**（V3-T6 移入）：官方自节流模式（LHT 先例）若未来被多个真实功能重复实现且产生可量化收益，再立票——挂真实需求信号，不预建 implementation、不提前扩约。

## Out of scope

- **BUE 自建 DLL scanner / 外部 module loader / 搬运 BepInEx 发现排序 / 第二套依赖求解器**：图级冻结（见 Notes）。
- **Contracts.dll 拆分的实施动作**：V3-T9 只裁决四条件现状与门禁；若四条件实证触发拆分，属目的地重绘，另立 effort。
- **提前扩张公开 Major 契约**：不为假设需求扩面。
- **LMN/SPF 传输层上游缺陷**：DEV-V2-25 Out of Scope 先例，对 SPF 项目同步观察，不入本图。
- **生产实施本身**：红测/候选/实机验收随 DEV-V3-* 届时产生，本图只产决策。
