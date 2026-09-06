# 定义开放协作、构建与三环境发布门禁

Type: grilling
Status: resolved
Author: GPT
Blocked by: GPT-06, GPT-08, GPT-11, GPT-13

## Question

在已确认“独立功能工程、PR 接入、构建/静态/契约检查、GPT 审核后端与共享契约、Gemini 审核前端、共同接口双方复核、同版本同哈希产物及单人/SteamP2PFriends/U3DS 独立验收”的前提下，精确决定目录清单、功能清单 schema、CI job、证据包格式、审批权限、失败处理和发布冻结规则。

## Comments

### 2026-08-24 Feature Definition Linker 第一轮

人工开发者接受 Q48—Q54：

- “功能定义编译”是领域片段编译、链接与产物组装的管线总称，不是万能 Compiler module。
- Definition Linker 只处理片段身份、schema 版本、引用目标、唯一归属、版本闭包与稳定链接；不解释设置、能力、依赖级联或 UI 领域语义。
- Identity、Module Binding、Relation、Settings、Capability、Presentation 与 Environment Intent 片段分别由其领域所有者裁决；其他 module 不得修补不属于自己的片段。
- Linker 只接受已裁决 typed fragments，不读取原始 JSON、源码、Git、审批或运行证据。
- 正式链接以全仓为原子闭包；required fragment 缺失、悬空引用、身份重复或版本闭包失败时不生成正式链接功能定义包，不能静默删除错误功能。
- required/optional 政策由片段所有者冻结，Linker 不猜测。
- 链接功能定义包保留已绑定的类型化片段与稳定句柄，不拍平成万能 DTO。
- Linker 结果只使用 `LinkSucceeded / LinkFailed`，不表达 Artifact、DLL、运行验收或 Release 状态。

### 2026-08-24 Consumer Facet Ownership

人工开发者接受 Q55—Q61：

- Lifecycle、Network、Settings、Frontend、Admission 与 Evidence Obligation facet 分别由对应消费者领域拥有；Linker 不实现消费者规则。
- Facet 只能确定性解释链接功能定义包中的事实，不能补写声明、修改版本、推断能力或把运行/验收状态写回定义。
- 不建立统一万能 Facet interface；共享部分只提供稳定只读句柄、类型化片段访问、Definition Set 身份和有界引用解析。
- 静态 facet 在构建期生成；依赖当前进程环境的 facet 在初始化时推导；网络协商、设置快照和生命周期状态属于运行/会话事实，不是定义 facet。
- 构建期静态 facet 失败阻止正式 Artifact；单功能环境不适用交给 Runtime Feature Admission；合法片段无法被核心 facet materializer 解释属于核心故障；第三方不得提交可执行 facet materializer。
- Facet 绑定 `FeatureId / DefinitionSetId / DefinitionDigest / FacetSchemaVersion / SourceFragmentDigests`；Definition Set 不一致时拒绝组合。
- 冻结 deletion tests：删除前端、新增网络字段、新增设置类型、修改生命周期政策或替换 Artifact 编码，均不得无理由迫使 Linker 改动。

### 2026-08-24 Behavioral Module Bootstrap

人工开发者接受 Q62—Q68：

- 废弃 `IFeatureModule.Describe()`、`IFeatureSettings.Describe(FeatureId)` 及运行时身份、依赖、设置 schema、能力声明通道；声明事实只来自链接功能定义包。
- Runtime Feature Admission 后由核心创建已绑定 FeatureId、版本、Definition Set、lifecycle generation、设置、日志、资源登记、事件和依赖能力视图的不可变功能作用域。
- 功能作用域内 Settings、Logger、Lifetime 等 interface 不再重复接收 FeatureId；模块可读取自身身份但不能选择或改写身份。
- 跨功能访问只允许通过已链接依赖与受限 capability seam；不提供全局模块查找器，也不能按任意 FeatureId 取得其他功能内部状态。
- 入口实例化与 Start 分开；唯一核心入口只由 Module Binding 指定，禁止扫描与猜测；实例化异常属于单功能启动失败。
- 可预期启动拒绝返回结构化结果；未处理异常按 GPT-09 首次异常隔离；成功只表示本次运行进入 Running。
- 核心行为入口与客户端 UI 入口分离、资源作用域分离；核心隔离时 UI 必须清理。U3DS 不得解析或实例化 UI 类型，真实环境仍需独立验收。

### 2026-08-24 自主循环优化裁决

人工开发者明确授权 GPT 按 `improve-codebase-architecture` 自主执行扫描、deletion test、二次优化和审计循环，不再逐项询问底层 interface。

- 否决浅层 Artifact Assembly seam；保留 Definition Artifact Format module。一个 canonical Definition Artifact 内含 header、section directory、linked definition、runtime registration、client UI registration 和其他 typed sections。
- DefinitionSetDigest 表达链接语义集合身份，ArtifactPayloadDigest 表达按规范排除规则计算的编码 payload 身份；BuildIdentity、最终 DLL SHA-256、环境证据和发布授权由后续事实源维护。

### 2026-08-24 独立审计结论

自主循环优化经三轮独立审计最终 PASS：feature-scoped capability/events、程序集与定义产物配对、CoreShared/ClientUi U3DS 结构隔离及 ArtifactPayloadDigest 无环计算均已闭环。该轮审计时共享 interface 尚待 Gemini 复核；后续复核已在下一节完成。该 PASS 仅代表 Wayfinder 文档架构，生产构建与三环境运行均未开始。
- 定义、资格义务、证据案例、技术资格裁决和发布授权严格分层，任何一层不得冒充下一层。
- CI 降为编排 adapter，不拥有身份正则、关系语义、证据充分性或发布规则。
- Contribution Governance、Identity Event Projection、Manifest、Relation、Build Qualification、Evidence Case & Provenance、Qualification Evaluation、Release Claim & Authorization 与 Runtime Feature Admission 的裁决见 `../Contribution-Build-Release-Gates-Spec.md`。
- 共享 interface 已移除运行时 `Describe()` 第二事实源，改为 feature-scoped bootstrap；当时通过 `../handoffs/to-14-feature-definition-contract-review.md` 转交复核，现已获 Gemini 全量接受。

### 2026-08-24 Gemini 与人工开发者最终复核

- Gemini 完整阅读两份 GPT-14 规格与交接文档，接受 Definition Linker、consumer-owned facets、feature-scoped bootstrap、静态 Settings facet + 运行时快照以及 U3DS/UI 隔离设计，无阻断意见。
- Gemini 已回写 `Frontend-Architecture-Spec.md` §4.3，明确旧 `IFeatureSettings.Describe()` 被 GPT-14 Facet 管线取代。
- Gemini 确认前端无需在 Linker 之外新增共享字段。
- 人工开发者接受 GPT-14 全部决议和实施。

## Answer

完整决策见：

- `../Feature-Definition-Pipeline-Spec.md`
- `../Contribution-Build-Release-Gates-Spec.md`

GPT-14 冻结：领域-owned typed fragments、Definition Linker、consumer-owned facets、单一 canonical Definition Artifact、feature-scoped bootstrap、Contribution Governance、Build Qualification、资格义务、证据案例、技术资格裁决、Release Authorization 与 Runtime Feature Admission。

Gemini 与人工开发者已最终接受，独立架构审计 PASS，阻断项 0。该关闭只代表 Wayfinder 决策完成；仓库仍无生产实现、DLL 构建或 SP/SteamP2PFriends/U3DS 运行证据。



