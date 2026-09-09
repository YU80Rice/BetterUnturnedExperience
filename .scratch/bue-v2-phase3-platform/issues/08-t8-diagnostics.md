# V3-T8 BueDiagnostics 与诊断包自动化

- **Ticket**: V3-T8
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-10 八项裁决定音，范围收窄版）
- **Blocked By**: V3-R2
- **Map**: [map.md](../map.md)

## Question

阶梯外补位票（2026-09-09 用户裁定：诊断包自动化超出 BueRuntimeLog 局部修补，属独立平台能力，必须有独立决策票）。具名子问题 = **诊断包自动化**（对账残差落位）。

1. **范围**：BueDiagnostics 平台能力定界——结构化日志 / DiagnosticId / 状态投影之外，诊断包（证据采集/归档/命名/哈希/CaseId）自动化的目标形态；
2. **深度**：自动化做到哪一层才有实际收益（依 V3-R2 结论）——一键采集？会话内自动留档？还是仅采集器 API 供功能调用？不扩大为整个发布系统重做；
3. **生态接口**：诊断面要不要进公开契约（生态功能往诊断包里写自己的证据？）；
4. **留到后续**：哪些明确不做；
5. **契约影响**；
6. **同权影响**：官方功能与生态功能的诊断可见性是否同权（面板/日志/包内）。

事实输入：V3-R2（本票唯一前置）。主源：`docs/agents/auto-rm-test-sop.md`、`.scratch/bue-v2-phase2-official-adoption/research/2026-09-09-umm-diag-archive-inventory.md`、BueRuntimeLog / DiagnosticLogSink 现状。

## Answer

2026-09-10 用户+PM 八项裁决全部定音，**范围较首轮 grilling 收窄**：BUE 不建设日志复制器、诊断包打包器或面板导出动作——LogOutput.log 由 BepInEx 统一产生，UMM 负责人工导出 Player.log+LogOutput.log。deepening 目标=「各模块各自写日志、失败信息分散、摘要靠人工阅读」深化为统一 IFeatureLogger+有界摘要 module；BUE 负责结构化与聚合，UMM 负责人工导出，职责清晰不重复建设。

### 1. 范围定界 = 两块自动化+一项外部人工职责

**BUE 自动负责**：①IFeatureLogger 接线；②诊断事件结构化记录与摘要聚合。**UMM/用户人工负责**：导出 Player.log+BepInEx/LogOutput.log 形成诊断包。BUE 职责链：模块产生诊断→写入 BepInEx LogOutput→同时进 BUE 内部摘要聚合器→UMM 人工导出时自然带走。**T8 不含**：复制 LogOutput/Player.log、创建诊断包目录、压缩、上传、归档、登记 CaseId/RELEASES。

### 2. 日志导出与摘要目标形态

人工导出沿用 UMM 现行流程（Player.log+LogOutput.log），BUE 不重复实现。BUE 运行时自动维护有界摘要：`DiagnosticId+FeatureId+Level+EventName+Count+FirstSeen/LastSeen`，以有限结构化行写入 LogOutput（`BUE diagnostic-summary featureId=… diagnosticId=… count=… firstSeen=… lastSeen=…`）。约束：不逐条重复输出、不因摘要制造刷屏、内存不无限增长、重启不假设摘要持久、UMM 导出即带走、**摘要不是验收授权、不替代原始日志**。推荐输出时机：启动完成/功能 Isolated-Recovered/网络代际结束/用户主动触发诊断刷新（如存在）。本票不新增 BUE 文件。

### 3. 生态 Logger 接口与附件

生态用 `IFeatureLogger.Info/Warning/Error` 三方法窄面：进同一 BepInEx LogOutput、进同一摘要聚合器、UMM 导出时与官方一起收集、不因生态来源被静默过滤、不需要生态专用文件。本阶段不提供：诊断附件 API、生态文件注入、自定义 zip 片段、远程上传。**DiagnosticId 前缀**：`BUE-*` 为平台保留前缀；生态诊断码用自己 FeatureId 派生前缀（例 `com.example.inventory-001` 或 SDK 统一合法格式）；生态使用 `BUE-*`=**拒绝写入+结构化拒绝诊断**（与 T2 官方 FeatureId 保留段同一治理哲学）。

### 4. 与前序票打通（自动写入 LogOutput+摘要聚合，不做独立采集包）

T8 自动接收并聚合：①T4 功能级隔离（Isolating/Isolated/Stopped+对应 DiagnosticId）；②T5 网络链路健康（degraded/recovered/generation/last send result/consecutive failures）；③T4 状态投影（FeatureStatusView/StateRevision/FeatureId/LifecycleGeneration）。路径：IFeatureLogger→BUE diagnostic sink→LogOutput 结构化行→摘要聚合→UMM 人工导出。状态快照 JSON 行还是多行结构化记录留实施票；本票只冻结「不新增 BUE 独立导出包」。

### 5. 实施深度 = 现状加固四件套

①Logger 接线（每模块绑定自身 FeatureId 的 logger view、自动带 FeatureId/eventName/DiagnosticId、生态与官方同一 sink、Logger 异常不得反向破坏模块运行）；②摘要聚合器（按 DiagnosticId 聚合/按 FeatureId 分域/按级别区分/计数与最近时间；必须有最大条目数、最大字符串长度、过量降级策略、代际或会话隔离、摘要输出限频、不保存敏感 payload）；③结构化日志输出（模块启停/隔离恢复/网络 degraded-recovered/设置提交拒绝/诊断前缀拒绝/资源清理异常/事件回调异常/主线程 dispatcher 拒绝——UMM 仅凭现有导出文件即可看到必要上下文）；④红测+SDK 登记（生态 Logger 写入→结构化行→摘要计数增加→重复不刷屏→UMM 导出可含摘要→容量受限→损坏输入不拖垮模块→BUE-* 生态前缀被拒→官方生态同 sink）。

### 6. 留到后续（「导出」彻底移出 BUE 范围）

UMM 诊断包自动化、诊断附件 API、自动压缩上传、RELEASES 草稿自动生成、远程诊断、面板实时诊断视图、Logger 扩展字段、摘要跨重启持久化、健康阈值可配置化、机器可读独立诊断文件——均可另案，不属于 T8 交付。T8 只把 BUE 做到：**日志写得规范→摘要自动聚合→UMM 人工导出即可带走**。

### 7. 契约影响 = Minor 加性（导出器不进生态契约）

入冻结面：①IFeatureLogger 恒 null→可用（T2 预授权兑现，与契约版本同步）；②Logger 三方法语义；③`FeatureId+eventName+DiagnosticId` 结构化字段；④`BUE-*` 平台保留前缀；⑤生态诊断码命名规则；⑥摘要=运行时诊断可见性非验收授权；⑦BUE 写 BepInEx LogOutput 的协作语义。不入冻结面：BueRuntimeLog/DiagnosticLogSink 类本体、摘要聚合器 implementation、UMM 导出器、诊断包目录结构、压缩/上传工具。与 T2..T7 同批共用 **2.1**，晚则顺延 **2.2**。

### 8. 同权检验（「同一包」改写为「同一日志出口与同一摘要机制」）

①官方/生态同一 Logger seam：同一 LogOutput、同一摘要聚合器、同一字段与限频规则、不因来源过滤；②官方先行消费：至少一个官方功能诊断行改经 Logger view（官方功能→IFeatureLogger.Warning/Error→BUE sink→LogOutput→摘要）——不能只保留私有 Logger.LogWarning 路径而宣称已消费；③NoOp probe：Register→取 IFeatureLogger→写 Info/Warning/Error→摘要计数变化→结构化行可见→BUE-* 前缀拒绝→停止后不再写入（NoOp 不负责压缩复制建包）；④诊断可见性同权：同一 LogOutput/同一摘要/同一 UMM 人工导出路径——不承诺 BUE 自己拥有独立诊断包文件。

### 本票不做

不复制 Player.log/LogOutput.log；不创建 BUE 诊断包目录；不自动压缩/上传；不自动登记 RELEASES；不提供诊断附件 API；不做远程诊断；不做实时诊断面板；不把 UMM 的人工导出职责搬进 BUE。

## Comments
