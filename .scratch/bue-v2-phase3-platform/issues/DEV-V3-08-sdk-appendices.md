# DEV-V3-08：SDK 契约文档附录总装（附录 A/B/C+自检清单+NoOp 统一 probe 扩链）

Type: task
Status: resolved（2026-09-11，双轴 R3 双 CLEAN 闭环；审计=audit/2026-09-11/DEV-V3-08/结单报告.md）
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-01、DEV-V3-02、DEV-V3-03、DEV-V3-04、DEV-V3-05、DEV-V3-06、DEV-V3-07（全部平台缝实施票）
Spec: `../spec.md`（「SDK 契约文档（V3-T9 → DEV-V3-08）」节）

## What to build

生态作者只凭一份 SDK 契约文档+一个活样板就能接入全部平台服务：附录 A（平台服务参考七节）、附录 B（诊断与身份码表）、附录 C（契约版本与迁移）成文；「生态 DLL 上架前自检清单」（11 项人工核对）可逐项打勾；NoOpFixture 升格为统一生态契约 probe，注册→Bootstrap→Events→TryTrack/Lifecycle→Network→HostTick→Settings→Logger→停止与隔离全链可运行、失败分 seam 可定位。

## Scope

- 正文八节冻结不动；新增附录 A（Admission/Events/Lifecycle/Network/HostTick/Settings/Diagnostics 七节）、附录 B（BUE-REG-001..010、BUE-PLATFORM-001/002、前缀纪律、FeatureId 保留段及合法/非法示例）、附录 C（2.0→2.1 条目、安全降级原则、Major 纪律、四条件门禁、RELEASES 注记要求）。
- T1..T8 移交总账逐条映射到附录 A/B/C 具体章节；结票前逐条核对无遗漏。
- Contracts 拆分四条件全部未触发→继续暂缓，逐条登记为门禁条款（定义/事实判定/触发信号/重评义务；先触发预判=①编译脱耦、③发布节奏分化）。
- 上架前自检清单 11 项（引用方式、身份合规、降级义务等人工核对项）。
- NoOpFixture 统一 probe 扩链（覆盖各票扩链清单+可用性矩阵票后终态路径）；probe 失败必须分 seam 可定位（注册/事件/Lifecycle/Network/Settings/Logger 各自独立判据与诊断行），不得全链 PASS/FAIL 遮蔽。
- SDK 文档随主 DLL 契约版本走，不独立发版；文档示例锚定 NoOpFixture 真实代码（双向）。
- 不做：SDK 项目模板；编译期验证工具；第二样板；独立 SDK 程序集。

## 验收条件

- [x] 附录 A/B/C 成文且与 T1..T8 移交总账逐条对得上（每条可指出落档章节）
- [x] NoOp probe 全链绿+分 seam 定位断言（各 seam 独立判据红测证明可定位）
- [x] 文档示例与 NoOpFixture 代码双向锚定（示例可编译、样板在文档有出处）
- [x] 双轴独立审查（每轮全新实例）CLEAN
- [x] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线；v8 交付包 2.0 基线保持原状，换新归 09 票）

## Comments

2026-09-11 结单（/implement 独立会话，红先行→双轴 R3 双 CLEAN）：

- **交付三件**：SDK 文档附录总装（+412/-0，正文八节零删除机器核验；A.1..A.7 七节、B.0..B.10 码表+两分类口径+前缀纪律+FeatureId 保留段合法/非法示例、C.1..C.6 含四条件门禁逐条与 11 项 `- [ ]` 自检清单）；NoOpFixture 升格统一生态契约 probe（冻结链序七缝步+ProbeStepOutcome NotRun≠Passed+StepMismatches 分缝详情+停止缝视图捕获/DisposalOrder 逆序账+一次红一缝旋钮=真实拒绝错置期望）；Plugin.Tests 新常跑组 8 子组+`--bue-v3-probe-red`（含「SDK 附录与活样板双向锚定」子组=结构/逐码/门禁/清单/九条示例行=fixture 源码逐字 机器判据）。
- **具名移交对账**：T9 裁决⑤ T1..T8 总账+01..07 各票具名移交逐条对上附录章节（结单报告 §3 对账表；02 逐码处理建议→B.1/B.3 处置列、04 GEN/CREATED 口径→B.5、05 八条+时钟码→A.5/B.6、06 Settings 逐码→A.6/B.7、07 LOG 码表+A.7+NoOp 全链+清单→B.8/C.6、01⑦ Identity 字段可用性→A.1 F2 段）。01⑦ 为 R2-Spec 独立重取证揪出、R3 复验通过——「结票前逐条核对无遗漏」的实效证据。
- **红绿链**：编译红 160（CS0103/0117/0246/1061，样例面回退观测）→行为红 1（文档未成文；红链期修红测自身缺陷一处具名=结构缺失时 Substring 异常冒充红因）→文档成文 ALL GREEN；F1（Standards P2 临时设置根泄漏→06 同形 finally 清理+残根清光）、F2（01⑦ 兑现+断言补全）。
- **双轴链（每轮全新实例）**：R1 Standards FINDINGS(1P2+6deferrable：设计对称×1/07 同构×1/单线程×2/LF 副作用×1/注释口误已入 F1×1)+Spec CLEAN(注记三：04「第 12 成员」按实写恰 11=合规处理/Logger 行级判据=void 面本分/A.5 示例走 05 支线)→F1 四修→R2 双 CLEAN(注记二)→F2→R3 双 CLEAN。全套 7/7 PASS+全方案 Rebuild 0 警 0 错（终版二进制）。
- **候选纪律**：不产候选 DLL、不更 RELEASES、不授 CaseId；正文 §7 契约版本登记保持 2.0（对外 2.1 翻转=09 整体候选，C.1 时序纪律成文）；证据 audit/2026-09-11/DEV-V3-08/（log/diff 磁盘归档不入库，结单报告+全套 txt+红绿 run 入库）。
- **具名移交下游**：09=三环境实机验收+唯一 2.1 候选+RELEASES 行（注记最低要求已落 C.5）+publish 交付包文档换新；面板原生启停按钮 UI=09（06 递延照登）；NoOp 全链 probe 与自检清单已在票内闭环，下游无本票新增欠账。
