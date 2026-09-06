# DEV-V2-14：平台——入站订阅升契约与 Network 注入（Q1+Q2，契约 Major 升级启动）

Type: task
Status: resolved（2026-09-06，双轴 R2 双 CLEAN 后结票；候选身份与链路见 `audit/2026-09-06/DEV-V2-14/DEV-V2-14-closing-report.md`）
Parent: spec.md（V2 第二阶段规格·三插件官方纳入与平台首公里）
Blocked by: 无（先行票，可立即开始）
Spec: `../spec.md`（「平台：入站订阅与方向语义」「功能获取网络入口」「网络模块停用时的方法级语义」三节）

## What to build

功能开发者（官方与生态同权）只凭公开契约就能收帧并拿到网络 API：从功能启动参数获得永非 null 的 `IBueNetworkApi`，按频道与入站方向（来自客户端 / 来自服务器）订阅回调，得到独立幂等的注销句柄；网络模块停用时订阅合法、入站为零、发送返回显式结果。本票同时启动第二阶段契约 Major 升级与冻结面变更登记流程。

## Scope

- 契约：`Subscribe(FeatureId, ChannelDirection, handler)` + `ChannelDirection` 枚举入 `IBueNetworkApi`（方向=入站帧来源，非本地角色）；`IFeatureBootstrap.Network` 注入（永非 null、未就绪返回显式结果、停止后句柄失效或可安全重复释放、不泄漏 Host/LMN/Unity 类型）。
- 运行时：按方向的双 handler 表（handler 表与频道注册解耦——订阅未注册频道合法，帧仅在频道注册且流量到达后派发）；handler 锁外执行；单 handler 异常不扩散；空 handler / 非法方向参数异常 fail-fast。
- 契约 Major 升级：功能注册 `MinimumBueContract` 对齐新版本；冻结面变更登记条目启动（①订阅+方向、②Network 注入；后续票逐条追加）。
- 停用语义（规格定稿）：停用时 Register/Unregister/Subscribe 正常工作、入站为零、`Sessions` 空快照、发送按既有枚举（空快照 → `NoSession`）、生命周期事件不触发、不新增查询面。

## 验收条件

- [x] 红测先行：订阅方向语义 / 独立幂等句柄 / fail-fast 参数 / 未注册频道合法 / 停用语义（订阅合法+入站零+`NoSession`）各红测先红后绿（编译红 `red-compile-build.log` + 运行时红 NotSupportedException transcripts + 绿 `green-*.log`×7）
- [x] `Network` 永非 null 与功能停止后句柄语义红测（`--bue-v2-network-injection-red`：fail-fast 非空 / 停用重启同实例 / 未就绪显式结果 / Dispose 幂等+Unregister 可释放）
- [x] 契约 Major 升级后全套测试 PASS（含契约断言工程），旧调用点全部对齐（7/7 PASS、0 警告 0 错误；无 (1,0)/2 参 Subscribe 残留）
- [x] 冻结面变更登记条目建立（①②）（`docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`「契约版本演化」节，当前契约 2.0）
- [x] 构建 0 警告；双轴独立审查（standards-reviewer / Spec-Reviewer）CLEAN（R1 双 NOT CLEAN → 修复轮 → R2 双 CLEAN）

## 实施结单（2026-09-06，agent /implement）

- **交付**：契约① `IBueNetworkApi.Subscribe(FeatureId, ChannelDirection, handler)` + `ChannelDirection`（byte，FromClients=0/FromServer=1）入冻结面；契约② `IFeatureBootstrap.Network`；组合根 `Core/Registration/FeatureBootstrap.cs`（纯组合记录，Network fail-fast 非空）；运行时按方向双订阅表+会话握手起源定方向+接收侧频道派发门+锁外 handler+单 handler 异常隔离+`SetModuleActive` 停用语义；契约门槛 1→2，全部 `MinimumBueContract` 对齐 (2,0)；SDK 文档「契约版本演化」节登记①②。
- **双轴 R1→R2'→R2''**：Standards R1 BLOCKING（停用×Hello/Ack 重入锁不重检 moduleActive 的竞态窗）→ 三 handler 锁内重检修闭；锁探针换外线线程取证（同线程 Monitor 可重入不可证伪）。Spec R1 GAP×3 → 停止语义对齐冻结析取原文+Unregister 断言 / 启动路径具名延期（属 21/22）/ 证据归档。R2 因继承 R1 审查上下文（违反 Fresh-instance 规则 425c2aa）**整轮作废**；R2'（全新实例）：Standards CLEAN / Spec BLOCKED（3×BLOCKER 经裁定为越界发现——魔数 BUE1=18 票、Sessions 收窄=16 票、Ack 匹配=17 票，均为下游票面 Scope 原文）；R2''（全新实例，仅 Spec 轴）核验裁定成立 + scope 内重审 **CLEAN**。**fresh-instance 链 R1→修复→R2'→裁定→R2'' 闭环**。具名延期六项见结单报告。
- **候选身份**：`BetterUnturnedExperience.dll` SHA-256 `8d044c7a6889a9cab939130889313af6980a6971b470e4c1a4d85a442c7a2f7b`（278016 字节，两轮 `-t:Rebuild` 逐字节一致）。不继承 12/13 发布批准。
- **边界声明**：方向=入站帧来源由会话握手起源承载（发起方入站 FromServer、应答方入站 FromClients）；「重新启用按自动握手重建会话」的自动握手本体属 DEV-V2-17，本票以客户端再握手覆盖「已挂订阅无需重挂」。
- **下一步**：前沿移交 DEV-V2-16（发送语义/会话组播，依赖本票契约 Major 与登记流程已建立）与 DEV-V2-15（LIT 单人，零阻塞）。

## Comments

- **2026-09-06 主会话裁定传达 + R2 作废**：R2 复审轮以 SendMessage 续用 R1 审查子代理实例，违反 output-review-loop Fresh-instance 规则（提交 `425c2aa`：「每轮两个全新实例，禁止续用上一轮审查者——继承上下文使审查锚定自身早期分析，该轮作废，不计入 CLEAN 链」）。R2 的双 CLEAN 判定作废。
- **R2'（全新实例双轴，2026-09-06）**：Standards 轴 **CLEAN**（4×DEFERRABLE 与既有具名延期一致，停用窗口闭合、外线锁探针可证伪均独立核验）；Spec 轴 **BLOCKED**——3×BLOCKER（魔数 BUE2→BUE1 未改 / `Sessions` 未收窄 established / Ack 仍按「第一个未建立会话」）。报告归档 `audit/2026-09-06/DEV-V2-14/R2'-standards.md` / `R2'-spec.md`。
- **实施者裁定（2026-09-06，附于 R2'-spec.md 文末）**：三项 BLOCKER 对照工单拆分均为越界发现——魔数+全仓 BUE2 清扫=DEV-V2-18 Scope 第 19 行、Sessions 收窄 established=DEV-V2-16 Scope（登记条目③）、Ack 按 peer+代际=DEV-V2-17 Scope；「结单称 established-only」引证经全仓 grep 证伪（结单/票面/SDK 文档无此表述）。代码零变更，增量保持 `48937d2`，候选身份 `8d044c7a…2f7b` 不变。
- **R2''（全新实例，仅 Spec 轴，2026-09-06）**：**CLEAN**——第一层三项越界裁定逐行核实成立（含 established-only 引证不实的独立确认）；第二层本票 Scope 内重审无 BLOCKER（仅余两具名延期：生产启动路径属 21/22、Stop 自动失效取「可安全重复释放」分支）。报告归档 `audit/2026-09-06/DEV-V2-14/R2''-spec.md`。Standards 轴 R2' CLEAN 对同一 diff 继续有效。
- **最终判定**：fresh-instance 链 **R1 → 修复 → R2'（S:CLEAN/Spec:BLOCKED→越界裁定）→ R2''（Spec:CLEAN）** 闭环，双轴最终 CLEAN。规约依据 `docs/agents/output-review-loop.md`（Fresh-instance 规则版）。
