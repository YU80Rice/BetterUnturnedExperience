# GPT-Feature-Definition-Pipeline-Spec：功能定义、产物与资格义务管线

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT-14 Wayfinder 决策已由 Gemini 与人工开发者接受；实现与运行未验证**  
**方法: improve-codebase-architecture 循环扫描、deletion test 与独立候选审计**

## 1. 目标与证据边界

本规范消除 `feature.json`、运行时 `Describe()`、网络清单、设置描述和加载注册之间的多源功能事实，同时避免制造一个理解全部领域语义的万能 Compiler。

管线为：

```text
领域声明
  → 领域拥有的功能定义片段编译
  → Feature Definition Linker
  → 链接功能定义包
  → 消费者拥有的静态 facet
  → Definition Artifact Format
  → 单一定义产物
```

管线成功只证明定义闭包和规范编码成立，不证明源码可信、DLL 已构建、三环境已运行或获得发布授权。

## 2. 事实所有权

| 事实 | 唯一所有者 | 禁止写入的位置 |
| --- | --- | --- |
| FeatureId、slug、墓碑 | Identity Projection | 模块代码、网络协商、运行状态 |
| 依赖与替代关系 | Feature Relation | Lifecycle/Network 自行解释的副本 |
| 设置 schema | Settings | 运行时模块 Describe、前端控件 |
| 能力声明 | Capability/Network | 客户端自报授权、运行时扫描 |
| 表现元数据与 UI 入口意图 | Frontend | Core/U3DS 类型依赖 |
| 核心入口绑定 | Module Binding | 程序集扫描与类名猜测 |
| 环境目标与成熟度意图 | Environment Intent | PASS 状态和发布声明 |
| 当前 FeatureState、StateRevision 与状态事件 | ModuleRuntime/Lifecycle | Runtime Admission、规范功能定义 |
| 网络协商结果 | Session negotiation | 能力声明片段 |
| 当前设置值 | SettingsRuntime | 设置 schema 片段 |
| DLL SHA-256 与运行证据 | Evidence Case & Provenance | feature.json、定义产物 |
| 技术资格裁决 | Qualification Evaluation | maturity 字段、FeatureState |
| 发布授权 | Release Claim & Authorization | CI build status、技术 verdict |

## 3. 领域拥有的功能定义片段

首版片段族：

- Identity Binding fragment。
- Module Binding fragment。
- Dependency/Relation fragment。
- Settings Schema fragment。
- Capability Declaration fragment。
- Presentation Metadata fragment。
- Environment Intent fragment。

每个片段拥有者负责原始声明的语法、领域不变量、required/optional 政策、schema 演进和稳定 diagnostics。第三方只提交声明，不提交可执行 fragment compiler 或 facet materializer。

片段必须携带稳定 FeatureId、fragment kind、fragment schema version、source declaration digest 和领域编译结果。它不携带 GitHub/DNS 审批过程、运行状态、证据或发布结论。

## 4. Feature Definition Linker

Linker 只负责：

- 片段身份与 schema 支持范围。
- FeatureId 唯一归属。
- 跨片段引用解析。
- required fragment 完整性。
- 跨功能版本闭包。
- 全仓稳定排序。
- Definition Set 身份与链接摘要。

Linker 不解释：

- 设置值域与可见性规则。
- 能力方向、授权或网络协商。
- required dependency 的运行期级联。
- UI 表现和 U3DS 类型装载。
- 环境证据是否通过。

正式链接以全仓为原子闭包。required fragment 缺失、悬空引用、身份重复、schema 不支持或版本闭包失败时返回 `LinkFailed`，不生成正式链接功能定义包，也不能静默丢弃错误功能。成功只返回 `LinkSucceeded`。

## 5. 消费者拥有的 Facet

Lifecycle、Network、Settings、Frontend、Runtime Admission 与 Qualification 分别拥有自己的 facet 解释。Linker 不生成万能 DTO，也不拥有消费者规则。

Facet 只能确定性解释链接事实，不得补写声明、修改版本、推断未声明能力或把运行/证据状态写回定义。静态 facet 在构建期生成；环境 facet 可在初始化时结合可信进程事实推导；会话与运行状态不是 facet。

每个静态 facet 绑定：

- FeatureId。
- DefinitionSetId。
- DefinitionSetDigest。
- FacetSchemaVersion。
- SourceFragmentDigests。

不同 Definition Set 的 facet 不得组合。

Deletion tests：

- 删除前端不修改 Linker。
- 新增设置类型只修改 Settings fragment compiler、facet 与必要共享契约。
- 新增 capability 尾部字段不修改 Lifecycle 或 Linker。
- 替换产物编码不改变片段或 facet 语义。

## 6. Behavioral Module Bootstrap

废弃运行时声明通道：

- `IFeatureModule.Describe()`。
- `IFeatureSettings.Describe(FeatureId)`。
- 运行时依赖、设置 schema 和能力声明。

Module Binding fragment 在构建期把唯一核心入口与 FeatureId 绑定。Runtime Feature Admission 在可信批次中返回 `Admit(handle)` 后，ModuleRuntime 只保存 handle 并保持 `Discovered`；SettingsRuntime 必须先依据静态 Settings facet 完成迁移、校验并产出只读 enablement snapshot。随后 ModuleRuntime/Lifecycle 在唯一状态写入 seam 中结合该快照、动态依赖与政策决定 `Discovered → Starting/Disabled`。只有进入 `Starting` 后，核心才实例化入口并创建 feature-scoped bootstrap。Settings、Logger、Lifetime 和 owned Events 已绑定当前 FeatureId，调用时不重复接收任意 FeatureId。Dependency capability view 只接受当前功能已链接的本地 dependency id；事件发布使用当前功能 Event fragment 中预声明的 event id。

跨功能访问只允许通过已链接 required/optional dependency 和受限 capability view；不存在全局模块查找器。核心行为入口与客户端 UI 入口分别注册、分别拥有资源作用域；U3DS 路径不得解析或实例化 UI 类型。

## 7. Definition Artifact Format

不建立只负责拼接文件的浅层 Artifact Assembly seam。保留具有真实 depth 的 Definition Artifact Format module：构建端编码、运行端读取均经过同一格式 seam。

每个构建只生成一个 canonical 定义产物，最终嵌入唯一 DLL。其内部为：

```text
Header / manifest
Section directory
Linked definition section
Runtime registration section
Client UI registration section
Other typed facet sections
Integrity trailer
```

Runtime registration 与 Client UI registration 是同一产物的独立 section，不是并列发布文件。U3DS reader 可以跳过 UI section，并且跳过过程不得触发 UI 类型解析。

格式 module 负责 canonical encoding、稳定排序、section 边界、格式版本、结构校验、DefinitionSetDigest 和 ArtifactPayloadDigest。具体端序和数值编码在格式版本实现前冻结；当前只冻结：无时间戳、无本机绝对路径、无用户名、无随机值、无不稳定文件枚举顺序。

顶层摘要语义：

- `DefinitionSetDigest`：链接语义集合身份。
- `ArtifactPayloadDigest`：按固定排除规则计算的规范 payload/sections 内容身份。
- fragment/facet digest：section trace metadata。

最终 DLL SHA-256 和发布真实性不属于格式 module。BuildIdentity 由 Build Qualification 按无环顺序生成，格式 module 只提供规范编码与摘要输入。

## 7.1 Build Artifact Binding

格式完整性与当前程序集配对是不同事实。构建生成同一份不可变 `AssemblyBuildBinding`，分别嵌入程序集元数据/生成注册根和定义产物 header：

- BuildIdentity。
- DefinitionSetDigest。
- ArtifactPayloadDigest。
- ArtifactFormatVersion。

为避免摘要自引用，冻结生成顺序：

1. 生成 canonical header、section directory 与 sections；header 中 `ArtifactPayloadDigest` 和 `BuildIdentity` 槽位使用全零规范值。
2. 对“零值槽位 header + section directory + 全部 canonical sections”计算 `ArtifactPayloadDigest`；integrity trailer 不在覆盖范围内。
3. Build Qualification 以稳定 source snapshot、toolchain/reference identities、DefinitionSetDigest 和 ArtifactPayloadDigest 计算 `BuildIdentity`。BuildIdentity 不包含最终 DLL SHA-256。
4. 回填 ArtifactPayloadDigest 与 BuildIdentity，并写入只重复必要校验字段的 trailer；不得把回填后的完整容器哈希再次写回自身。
5. 最终 DLL 完成后计算 DLL SHA-256，只记录在 CandidateBuild/Evidence，不回填程序集或定义产物。

运行时 verifier 按同一规则把两个 header 槽位逻辑视为零，重算 ArtifactPayloadDigest，再比较程序集绑定与产物 header。

运行初始化首先经过窄的 Build Artifact Binding Verifier，比较当前程序集绑定与嵌入产物 header。它只证明“当前程序集与当前定义产物来自同一候选构建”，不证明签名真实性、发布授权或环境 PASS。任一字段缺失、不一致、payload 摘要失败或重复注册根冲突属于全局核心不变量损坏，进入 SafeMode。

Verifier 成功后才允许解析 runtime registration section 并进入 Runtime Feature Admission。

## 7.2 单 DLL 中的 UI 结构隔离

仅跳过 Client UI section 不足以证明 U3DS 安全。首版冻结以下结构规则：

- 功能源码清单明确分为 `CoreShared` 与 `ClientUi`；U3DS/core reference 编译检查只编译 CoreShared 闭包，最终单 DLL 聚合构建包含两者。
- Contracts、CoreShared、Module Binding、Runtime Admission 和 U3DS 可达入口不得在基类、interface、字段、属性、泛型约束、attribute、静态初始化器或方法签名中引用 Glazier/Sleek/客户端专属类型。
- ClientUi 实现类型为 internal、无 BepInEx/Harmony 自动发现 attribute，不作为核心入口，也不进入全程序集反射扫描。
- 禁止全程序集 `Assembly.GetTypes()`、全局 `PatchAll()` 或按类名扫描。入口与 Harmony patch 均由生成的显式表注册；ClientUi 表只在非 batch/headless 且 `ClientUiAvailable` 成立后解析。
- ClientUi 类型对客户端专属类型的引用限制在客户端激活路径的方法体或私有实现中；不得让 U3DS 可达调用图持有对应类型 token。
- 构建门禁包括：CoreShared 对 U3DS reference set 的独立编译、最终 DLL 的 U3DS 根可达 IL/type-reference 审计、客户端完整构建，以及同一最终 DLL 的真实 U3DS 加载/运行验证。

CoreShared 编译 PASS 只证明核心源码闭包；最终 DLL 静态可达审计仍不能替代真实 U3DS 运行。

生成产物进入中间目录且不提交 Git；仓库提交声明、schema、工具和小型 golden fixtures。CI 从干净 checkout 做确定性双构建比较。格式编码全成全败；临时目录、原子公布和旧输出清理由 build orchestration 负责。

## 8. 资格义务推导

Qualification Obligation Derivation 消费链接事实和项目级资格政策，只产生“必须证明什么”。它不读取日志、不判 PASS、不批准发布。

项目铁规要求所有框架及内置功能面向：

- 单人环境。
- SteamP2PFriends Host/Client。
- U3DS 环境。

第三方 `targets` 不能选择性退出产品级三环境义务；它只能表达适用角色和设计意图。UI facet 在 U3DS 可为 NotApplicable，但功能整体仍必须证明 U3DS 不解析 UI 类型且核心安全加载。

典型义务：

- 客户端与 U3DS 双引用集静态检查。
- 唯一 BepInEx 入口和单 DLL。
- U3DS 激活路径不解析/实例化 UI 类型。
- SP 运行案例。
- SteamP2PFriends 共享 CaseId 的 Host/Client 双端案例。
- U3DS headless 运行案例。
- 模块局部故障隔离。
- 所有运行案例绑定同一候选 DLL SHA-256。

## 9. 定义、义务、证据、Verdict 与授权

```text
规范功能定义
  → 资格义务
  → 证据案例
  → 技术资格裁决
  → 发布授权
```

- 定义表达功能是什么、意图支持什么。
- 义务表达某候选必须证明什么。
- 证据记录具体候选在具体场景的观察。
- 技术 verdict 为 `Fulfilled / Failed / Missing / Stale / NotApplicable`。
- 发布授权消费完整技术 verdict 与治理审批，决定能否发布或提升成熟度。

新 DLL SHA-256 使旧运行证据 `Stale/Mismatched`。缺少任一 P2P endpoint、两端 hash 不同、静态 U3DS PASS 但无真实 U3DS 日志，均不能得到对应运行义务 Fulfilled。人工批准不能补齐 Missing evidence；技术 PASS 也不自动授权发布。

## 10. CI 角色

CI 是编排 adapter，不是规则所有者。它调用领域片段编译、Linker、Facet、Artifact Format、Build Qualification 和 Qualification Evaluation，展示稳定 diagnostics，并按 PR/main/release-candidate/manual-evidence 触发类型阻断。

CI workflow 文件不得复制 FeatureId 正则、关系语义、证据充分性或发布裁决逻辑。

## 11. GPT-14 配套决策落点

以下配套决策已由 `Contribution-Build-Release-Gates-Spec.md` 冻结：

- Contribution Governance：命名空间、身份事件、审批政策与贡献许可。
- Build Qualification：源码快照、工具链/引用集、确定性构建和 CandidateBuild。
- Environment Qualification：资格义务、证据案例与技术资格评估。
- Release Claim & Authorization：发布冻结、成熟度提升和人工批准。
- Runtime Feature Admission：消费已验证定义产物与可信环境事实，原子返回可信批次中的完整 per-feature decisions 或核心故障升级；不直接写 `FeatureState`。

共享契约、生命周期和设置规格已按适用范围同步 feature-scoped bootstrap、dependency capability、事件所有权和静态 Settings facet；Gemini 已完成前端消费复核并回写其前端规格 §4.3。网络能力协商与 GPT-13 失败表现的既有权威边界保持不变。

## 12. 证据边界

本规范是 Wayfinder 决策。HTML 报告、Markdown、deletion test 和独立审计只证明架构决策经过审查；不证明 C# 实现、确定性构建、DLL、SP、SteamP2PFriends 或 U3DS 运行。

