# DEV-V3-07：BueDiagnostics 统一诊断（Logger 接线+有界摘要+BUE-* 前缀纪律）

Type: task
Status: resolved（2026-09-11 红绿链闭环+双轴 R2 双 CLEAN，登记正文见 Comments）
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-01（注册桥与 Bootstrap 基线）、DEV-V3-03（BueLifecycle）、DEV-V3-04（BueNetwork）
Spec: `../spec.md`（「诊断（V3-T8 → DEV-V3-07）」节）

## What to build

生态作者的诊断行进入与官方同权的证据链：经注入的 `IFeatureLogger` 写三方法窄面日志，行进同一 LogOutput 与有界诊断摘要，不被静默过滤；玩家被 UMM 导出的日志里直接看到结构化诊断与摘要。原始日志导出仍归 UMM 人工流程——BUE 不建日志复制器、采集器或面板导出动作。

## Scope

- `IFeatureLogger` 接线（可用性矩阵行，红线钉「接线前 null+接线后可用」两侧）：每模块绑定自身 FeatureId 的 view，三方法窄面（Info/Warning/Error）；Logger 异常不得反向破坏模块。
- 有界诊断摘要：按 DiagnosticId 聚合（FeatureId/级别/计数/首末时间），容量受限/输出限频/重启不持久；结构化行写入 BepInEx LogOutput；摘要≠验收授权（CaseId/RELEASES 仍人工）。
- `BUE-*` 平台诊断前缀保留；生态诊断码用 FeatureId 派生前缀；冒用=拒绝写入+诊断。
- 统一 sink 收编：T4 隔离/T5 链路健康/状态投影进统一诊断 sink（分 seam 判据可定位，不互相遮蔽）。
- 不做：日志复制器/诊断包打包器/面板导出动作；诊断附件 API；诊断实时视图（均为后续候选或 Out of Scope）。

## 验收条件

- [x] 红测先行：sink 捕获（Logger 行→LogOutput 结构化行→摘要计数；BUE-* 前缀拒绝；容量受限降级），各先红后绿（先例=DiagnosticLogSink/Recorder seam 与 `--*-red` 锚点）
- [x] 矩阵接线两侧红测：Logger 接线前 null+接线后可用；Logger 异常不反向破坏模块红测
- [x] 官方先行消费锚：官方功能走注入 Logger view 断言（非内部控制面）
- [x] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [x] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）

## Comments

2026-09-11 结单登记正文（逐条含锚映射，防伪造口径=行为面→实现落点→锚）：

**零新增契约面（T2 预授权兑现）**：`IFeatureLogger` 三方法窄面与 `FeatureBootstrap` logger 参数=既有形状，本票仅兑现可用性矩阵行「null→DEV-V3-07 后可用」。矩阵两侧=翻转三处既有 Logger==null 锚（01 矩阵组/生命周期矩阵接线侧组/06 矩阵两侧组→非 null，真实 StartCatalog 每功能一律接线、无 facet 门）+手工未接线组装=null 阶段基线侧锚。Contracts.Tests 形状锚：IFeatureLogger 恰三方法（void，参数 2/3/4）、bootstrap.Logger 成员类型、IFeatureBootstrap 仍恰 11 成员（隐式扩面必红）。

**生产面**：Core 新 `Diagnostics/DiagnosticRuntime.cs`（public 非 SDK=宿主组合，registry/dispatcher 同先例：DiagnosticLevel 枚举、FeatureLoggerView、DiagnosticSummaryEntry 只读值投影、聚合器+代际账）+Plugin 新 `BueDiagnosticsRuntime.cs` 组合根（级别映射 Info/Warning/Error→BueRuntimeLog Load/Warn/Error=正常播放日志可见、非 Debug 静默通道；fallback=ErrorFriendly 单前缀；Clear 测试缝）。

**Logger view 行为面八条**：三方法 void=无报错面，隔离义务全在 view 侧——sink 恒抛与主+fallback 双故障均不外抛（故障观察=BUE-LOG-005 fallback 恰一条/episode latch）；每行盖自身 FeatureId+代际；代际撤账后（停用/隔离/start 失败×2/enable 失败×2/宿主停止）写入=不产模块行+BUE-LOG-004 显式留痕一条（reason token 区分 write-boundary/host-stopped）+静默计数；OpenGeneration 重臂=宿主停止是边界非死刑（同进程 reload 锚，04 同构）；前缀纪律按保留段身份判定（`OfficialFeatureIdentity.IsReservedSegment`——段内白名单身份经登记桥准入才存在，治理哲学=T2 同一条），段外冒用 BUE-*=拒写+BUE-LOG-001、段内放行（官方先行锚 BUE-LIT-* 的通过性前提）；null/空白标识符=拒写+BUE-LOG-002；消毒=空白→下划线+截断（event 64/diagnosticId 128/fault 256 本票定值），条目键=截断后值，伪造换行不产第二行。Error 的 fault=「类型:消息」无堆栈（不保存敏感 payload）。

**有界摘要**：按 (FeatureId, DiagnosticId) 聚合（级别=max 所见/计数/首末 UTC 时间）；容量 128（本票定值，SummaryEntryCount 可观察）——溢出=模块原行照写（证据链不被掐）+BUE-LOG-003 观察恰一条+新码不聚合（内存有界）；摘要行=T8 冻结形 `BUE diagnostic-summary featureId= diagnosticId= [level=] count= firstSeen= lastSeen=`；输出限频=首见一条+每条目 30000ms 至多一条（本票定值，fake clock 锚；只限摘要行，原行逐条如常=限频≠过滤）；纯内存=重启不持久（新实例零条目锚）；**摘要≠验收授权**（无 CaseId/RELEASES 面）。决策半部单锁（门+消毒+前缀+聚合+latch 同锁段，时钟读与发射锁外=R1 修复）；聚合先于行发射（写失败计数仍如实）。

**统一 sink 收编**：生产双绑两处——生命周期机 sink（T4 隔离/状态投影 BUE-LIFE-* 行）+网络运行时 diagnosticSink（T5 链路健康/预算 BUE-NET-001/002/003 行），原行路径逐字不变（03/04 既有锚零变更），`AggregateHostLine` 按字段边界 token 聚合（diagnosticId/feature=，行内优先于回落归属；无码行不聚合不造静默条目）=分 seam 判据可定位互不遮蔽。R1 注记复验=设置拒绝/事件回调/dispatcher 拒绝行保留前票原缝（收编面以 spec 140 三缝为准）。

**官方先行消费锚**：LIT Start 捕获 `bootstrap.Logger`（nullable 阶段基线面，LIR MainThread 先例），启停两结构化行经注入 view（BUE-LIT-START/BUE-LIT-STOP）；中文人读行保留原通道。测试 setup 清 `WiredModule`（LIT ShuttingDown 单向旗标=既有冻结语义，全新模块如实演练启停——06 官方锚组先例）。

**NoOp 生态对照支线**：ProbeState 四字段+三方法各调一次（BUE-NOOP-* 码）。具名判断：NoOp=白名单官方样例身份（段内通过侧），生态前缀拒绝锚以 io.example 身份另证互不遮蔽；全链 probe→08 递延同前。

**红绿链**：编译红 CS0234×42+CS1061×4（red-plugin-diagnostics-compile.log）→首轮行为红 8（red-behavior-8-runs.log：拒绝行早退吞/摘断格式/fault 格式三根因）→绿；红链期修红测自身缺陷两处具名（组级宿主 shutdown 断言须在 Recorder 复位前；G9 需清 WiredModule）；全套 7/7 PASS+全方案 Rebuild 0 警 0 错。

**双轴链（每轮全新实例）**：R1 Standards FINDINGS（P1 OpenGeneration 不重臂 hostStopped=与 04 不同构 reload 后新代际全拒；P2 单调钟未归一 Stopwatch.Frequency；P2 fallback 双前缀；deferrable×5）+Spec CLEAN（注记①T8 §2 字段枚举含 EventName 而冻结行不含→从冻结行；注记②收编面=spec 140 三缝）→F1（P1 重臂+reload 新红锚两断言；钟归一；fallback 去重；latch 入锁+决策单锁；ExtractToken 字段边界；deferrable 3 条转修，2 条具名保留：收编一律 Info=按码语义分级属组合层解析过度设计，string 标识面=诊断身份本 string 域）→R2 双 CLEAN（Standards 逐条核 R1 七项+增量终审无新违规；Spec 独立重取证+R1 两注记复验成立+F1 无超面）。

**具名移交**：附录 B 码表补 BUE-LOG-001..005+CREATED/GEN 观察行、附录 A Diagnostics 节（view 语义八条/限频数值/消毒形制/收编面）=08；NoOp 全链 probe 扩与上架自检清单=08；诊断实时视图/导出自动化=spec 已钉 Out of Scope 不再移交。

**候选纪律**：不产候选 DLL、不更 RELEASES、不授 CaseId；证据 `audit/2026-09-11/DEV-V3-07/`（log/diff 磁盘归档不入库，结单报告+final-fullsuite/green-red txt 入库）。
