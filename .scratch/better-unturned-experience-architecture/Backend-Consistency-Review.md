# GPT-Backend-Consistency-Review：后端与共享契约一致性复核报告

**作者: GPT**  
**日期: 2026-08-24 14:16（Asia/Shanghai）**  
**复核范围: GPT 后端、共享契约、公共基础设施及其与 Gemini 消费面的交接**  
**判定: PASS（第六轮独立终审通过，阻断项 0）；仍待前端报告与最终联合复核**

## 1. 证据边界

本报告只裁定 Wayfinder 文档的一致性、事实所有权、interface seam 与决策闭包。它不表示：

- 生产代码或项目已存在；
- `BetterUnturnedExperience.dll` 已构建；
- SP、SteamP2PFriends Host/Client 或 U3DS 已运行通过；
- GPT-12 HTML/Node 原型等价于生产 C# 零分配或游戏手感证据；
- 当前 Draft interface 已成为 Stable ABI；
- 已满足发布授权。

在 Gemini 前端一致性报告到达并完成最终联合复核前，项目继续停留在 Wayfinder，不进入 `/to-spec` 或生产开发。

## 2. 复核材料

核心事实源：

- `CONTEXT.md` 与 `map.md`；
- `Shared-Contract-Spec.md`；
- `Backend-Architecture-Spec.md`；
- GPT-09 至 GPT-14 的六份详细规格；
- GPT-01 至 GPT-16 决策票与 GPT/Gemini handoff；
- GPT-05 至 GPT-07 研究材料；
- Gemini 前端规格仅作为消费面交叉检查，不在本次复核中改写。

复核方法：

1. 对照事实所有权检查声明、运行状态、会话状态、证据与发布授权是否混用。
2. 对照 GPT-14 检查 GPT-08～13 是否残留运行时 `Describe()`、原始 manifest 解释或平行 Catalog/Gate/Permit 链。
3. 对照 GPT-09 检查隔离阈值、状态转换、依赖级联与清理完成语义。
4. 对照 GPT-10/11/13 检查设置 revision、连接 generation、RequestId、超时和网络降级。
5. 对照 GPT-07/08/12/13 检查库存原版权威链、候选预览与无逐次 ACK 约束。
6. 对照 GPT-14 检查 CandidateBuild、Definition Artifact、环境证据与发布授权的事实序列。

## 3. 已发现并修正的问题

| 编号 | 严重度 | 原问题 | 修正结果 |
| --- | --- | --- | --- |
| BCR-01 | 阻断 | 后端总纲仍让 ModuleRuntime 自行“发现/注册/校验身份并构建依赖图”，与 GPT-14 的 Definition Artifact + Runtime Feature Admission 重复解释事实。 | ModuleRuntime 现只接收 admitted feature handle 和已链接 Lifecycle facet；禁止重新解析 manifest、目录、身份和入口。 |
| BCR-02 | 阻断 | SettingsRuntime interface 仍声称运行时读取“设置描述”，会恢复被 GPT-14 废弃的动态描述第二事实源。 | 静态描述明确归构建期 Settings facet；SettingsRuntime 只拥有快照、事务、revision、权威和持久化值。 |
| BCR-03 | 阻断 | 后端故障表写成“连续失败”才隔离，与 GPT-09 首次越界未处理异常立即隔离冲突。 | 统一为首次未处理异常越过模块 seam 即进入 `Isolating`。 |
| BCR-04 | 高 | 后端持久化把核心清单和 Settings Schema 当作运行时可变文件，与单一定义产物冲突。 | 身份、入口、链接定义与 Schema 固定在嵌入 DLL 的 Definition Artifact；仅设置值按作用域持久化。 |
| BCR-05 | 高 | GPT-09 仍把 required DAG、cycle 和版本解释写成 Lifecycle 运行时职责，与构建期 Linker/Admission 重叠。 | 静态图与版本由 fragment compiler/Linker/Admission 裁决；Lifecycle 只做 admitted handle 与拓扑防御性不变量检查和运行状态级联。 |
| BCR-06 | 中 | `GPT-16` 保持 open，但地图声称只剩最终一致性复核，形成阶段状态冲突。 | GPT-16 已 resolved：不新增第二个产品参考功能；“更好的物品交互”继续作为唯一首发参考实现，轻量 sample 只能是不进入 DLL 的文档/测试夹具。 |
| BCR-07 | 中 | 共享契约前端消费顺序仍使用“描述模型”，未明确静态 facet + 动态 snapshot。 | 改为构建期静态 Settings facet 生成控件骨架，运行时完整快照驱动值、权限和 revision。 |
| BCR-08 | 中 | 旧同步报告仍宣称 GPT-10～14 未完成，容易被误作当前状态。 | 原报告已显式标注为历史快照，并由本报告取代。 |
| BCR-09 | 阻断 | 第一轮独立审核发现 Runtime Admission 与 Lifecycle 都可能写 `Incompatible/Disabled`，同一 `FeatureState` 缺少唯一写入 seam。 | Admission 只返回不可变 `AdmissionEvaluationBatch`；ModuleRuntime/Lifecycle 为全部功能建立 `Discovered` 并唯一生成状态、revision 与事件。 |
| BCR-10 | 阻断 | 第二轮独立审核发现 `RejectDisabled` 无 admitted handle，却允许 `Disabled → Starting`，导致默认关闭功能无法启用。 | decision 改为判别联合：`AdmitEnabled(handle)`、`AdmitDisabled(handle, reason)`、`RejectIncompatible(reason)`；全局 `CoreEscalation` 不产生功能记录。 |
| BCR-11 | 阻断 | 第三轮独立审核发现 Admission 试图基于尚未由 SettingsRuntime 迁移/校验的用户设置与政策产生 `AdmitDisabled`。 | Admission 只输出 `Admit(handle)` 或 `RejectIncompatible`；SettingsRuntime 随后产出 enablement snapshot，Lifecycle 再唯一决定 `Starting/Disabled`。 |
| BCR-12 | 阻断 | 第四轮独立审核发现 Feature Definition Pipeline 的 Behavioral Bootstrap 仍允许取得 handle 后直接 `Starting`。 | 同步为保存 handle 并保持 `Discovered` → SettingsRuntime enablement snapshot → Lifecycle 决定 `Starting/Disabled` → 仅 Starting 后实例化入口。 |

## 4. 一致性矩阵

| 领域 | 唯一事实源/拥有者 | 运行时消费者 | 复核结论 |
| --- | --- | --- | --- |
| FeatureId、slug、墓碑、命名空间 | Identity Event Ledger + Projection | Definition Linker / Runtime Admission | 一致 |
| 依赖与替代关系 | Relation fragment compiler | Linker、Lifecycle facet | 一致；Lifecycle 不重解释 |
| 功能声明与入口 | 领域 fragments + linked definition | Definition Artifact / Runtime Admission | 一致；无反射发现 |
| 生命周期状态 | GPT-09 ModuleRuntime | 前端只读状态投影 | 一致；首次异常隔离 |
| 设置 schema | 构建期 Settings fragment/facet | 前端静态骨架、SettingsRuntime 校验 | 一致 |
| 设置当前值/权限/revision | SettingsRuntime | `FeatureSettingsSnapshot` | 一致；完整快照为最小收敛单位 |
| 网络能力 | Capability facet + 服务端会话裁定 | NegotiatedFeatureView | 一致；LMN 不承担认证/库存权威 |
| 库存落点 | GPT-12 本地 evaluator | Gemini 预览层 | 一致；Local-Fit Priority |
| 库存最终事实 | Unturned 原生 `sendDragItem → ReceiveDragItem` 与原生投影 | 前端 AwaitingProjection | 一致；无自定义逐次 ACK/回滚 |
| 定义产物完整性 | Definition Artifact Format + assembly binding | Runtime Feature Admission | 一致；全局/局部故障分级 |
| 构建事实 | CandidateBuild | Qualification modules | 一致；构建不等于运行 PASS |
| 环境验收 | Evidence Case + Qualification Evaluation | Release Authorization | 一致；SP/P2P/U3DS 独立且同 DLL hash |

## 5. 关键不变量复核

### 5.1 公共框架

- 模块声明只有构建期一条事实链：domain fragment → Linker → linked definition → facets → Definition Artifact。
- Runtime Admission 是身份、完整性、入口与环境准入的唯一决策 seam；它不写 `FeatureState`。ModuleRuntime/Lifecycle 是状态、revision 与状态事件的唯一写入 seam，不建立平行 Gate 或可伪造 Permit。
- 单模块错误只做局部拒绝/隔离；影响范围不可确定的定义产物或核心不变量损坏才进入 SafeMode。
- UI 类型通过 `CoreShared` / `ClientUi` 结构隔离；U3DS 不扫描、不解析、不实例化 UI 注册 section。

### 5.2 更好的物品交互

- evaluator 只生成预览及原版提交参数，不授予库存权限。
- Local-Fit Priority 为：局部当前 → 局部旋转 → 全局当前 → 全局旋转。
- 不自动交换、不自动重排；无候选时取消增强提交并保留原位置。
- `AwaitingProjection` 的 2 秒是视觉预算，不是拒绝、重试或回滚事实。
- 原生库存投影始终是唯一库存事实；迟到投影静默接受。

### 5.3 设置与网络

- 静态 Settings facet 和运行时 `FeatureSettingsSnapshot` 是两类输入，不是竞争的声明事实源。
- `RequestId` 只提供同一连接代际内的事务幂等；revision 决定快照新旧；connection generation 隔离重连。
- 3 秒只重试一次同 RequestId，8 秒使用新 RequestId 请求 `0x0104` 完整快照；无第三次重试和离线队列。
- LMN 频道接受不等于应用能力 Ready，更不等于身份认证或权限授予。

## 6. 非阻断观察，交给最终联合复核

1. Gemini 前端规格早期章节仍出现旧名 `IFeatureSettings`，但 §4.3 已声明旧 `Describe()` 被 GPT-14 facet 管线取代。是否做全文术语清理由 Gemini 的前端一致性报告决定，GPT 未直接修改其文件。
2. 历史 handoff 与 resolved ticket 保留被否决的 `CommitItemPlacementCommand`、旧 interface 名称等上下文；这些文档必须继续被视为历史审查记录，不能高于当前规格和地图。
3. Gemini-01～03 仍为 open 属于前端实施前沿，不阻止本次“后端一致性 PASS”，但最终联合复核需确认它们不会被误判为 Wayfinder 未决产品决策。

## 7. 验证记录

- 旧后端职责关键词复扫：未发现 canonical GPT 规格继续要求动态 `Describe()`、连续 N 次异常、运行时 manifest 解释或平行 Permit 链。
- 决策状态复扫：GPT-01～GPT-16 均为 resolved；Gemini-01～03 保持前端 open。
- GPT-12 Node 原型回归：8/8 PASS；此结果仅为原型行为回归，不升级为生产或环境证据。
- 生产构建：N/A。当前仓库没有生产工程或 build command，本次只修改 Markdown 决策文档。
- 独立审核第一轮：FAIL，发现 Admission/Lifecycle 双写状态阻断项；已按单写者 `AdmissionEvaluationBatch → ModuleRuntime` seam 修复。
- 独立审核第二轮：FAIL，发现 `RejectDisabled` 无 handle 与 `Disabled → Starting` 冲突；已拆分准入成功但暂不启动与真正不兼容。
- 独立审核第三轮：FAIL，发现静态 Admission 与动态 enablement 混合；已拆成 Admission → Settings bootstrap → Lifecycle transition。
- 独立审核第四轮：FAIL，Feature Definition Pipeline 遗留绕过 Settings bootstrap 的旧时序；已同步修复。
- 独立审核第五轮：FAIL（仅报告阻断）；canonical 架构已通过，但本报告标题与最终判定仍引用第四轮，已修正。
- 独立审核第六轮：PASS，阻断项 0；canonical 架构、报告轮次与证据边界一致。

## 8. 最终判定

**后端与共享契约一致性：PASS，阻断项 0。**

前五轮发现的架构与报告问题均已修订，第六轮独立终审通过。GPT-owned canonical 文档现达到后端一致性 PASS。

下一步不是进入开发，而是等待 Gemini 的前端一致性报告。收到后，GPT 将把两份报告与 canonical 文档重新交叉，出具最终 Wayfinder 联合复核结论；只有该结论为 PASS 且人工授权，才推进 `/to-spec`。

