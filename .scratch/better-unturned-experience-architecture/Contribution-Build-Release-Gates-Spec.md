# GPT-Contribution-Build-Release-Gates-Spec：开放贡献、构建资格与发布门禁

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT-14 Wayfinder 决策已由 Gemini 与人工开发者接受；实现与运行未验证**  
**方法: improve-codebase-architecture 循环扫描、deletion test、独立子智能体审计候选**

## 1. 目标

为开放多创作者仓库建立可追溯的贡献、身份、构建、环境验收和发布流程，同时保持以下事实严格正交：

```text
贡献获准
定义闭包成立
候选 DLL 构建成功
候选产物身份确定
各环境技术资格满足
人工发布授权
```

任一事实都不能冒充后一事实。CI 是编排 adapter，不是规则所有者。

## 2. 仓库结构

```text
/
├── features/
│   └── better-item-interaction/
│       ├── feature.json
│       ├── src/
│       ├── tests/
│       └── README.md
├── registry/
│   ├── events/<event-id>.json
│   ├── namespace-claims/<claim-id>.json
│   └── schemas/
├── src/
│   ├── Contracts/
│   ├── Core/
│   └── Aggregate/
├── tests/
├── tools/
├── docs/
├── audit/
└── .github/
```

第三方功能只通过 Pull Request 以源码接入 `features/<slug>/`。V1 不加载外部功能 DLL，不接受运行时插件目录扫描。聚合工程一次编译为唯一 BepInEx DLL。

## 3. Contribution Governance module

### 3.1 拥有的事实

- 命名空间 claim 与验证证据引用。
- 稳定治理角色与审批 attestation。
- Feature identity 事件是否获准进入投影。
- PR 路径所有权与贡献许可确认。
- 弃用、撤销弃用、移除和 namespace delegation 的治理裁决。

它不拥有功能运行状态、构建结果、环境 PASS 或发布授权。

### 3.2 真实 adapters

- GitHub owner/organization verification adapter。
- DNS/domain verification adapter。
- Project-delegated community namespace adapter。

所有权验证不授予代码信任、运行权限、玩家权限或发布权限。

### 3.3 稳定角色

机器规则使用角色，不硬编码 GPT、Gemini 或个人姓名：

- `contract-maintainer`。
- `frontend-reviewer`。
- `feature-maintainer`。
- `namespace-owner`。
- `human-release-approver`。

具体人员映射由 CODEOWNERS/治理配置维护。共享契约变化需要 contract maintainer 与 frontend reviewer 双方复核；正式 Release 还需 human release approver。

### 3.4 高风险事件

以下至少需要两项独立 attestation，其中必须包含人工维护者批准：

- 命名空间首次登记。
- FeatureId 移除。
- `DeprecationRevoked`。
- 命名空间委托或撤销。
- removed identity 的 successor 目标变更。

审批记录包含角色、主体、策略版本、证据引用、变更摘要 digest 和原因。GitHub 评论不能成为唯一长期记录。

### 3.5 许可

仓库采用 MIT License。`CONTRIBUTING.md` 明确：提交者保证有权贡献，并同意按项目许可证发布。V1 不引入外部 CLA 系统。

## 4. Feature Identity Event Ledger 与 Projection

### 4.1 身份规则

官方内置命名空间：

```text
io.github.yu80rice.betterunturned.*
```

首个功能：

```text
io.github.yu80rice.betterunturned.iteminteraction
```

FeatureId：

```regex
^[a-z][a-z0-9]*(\.[a-z0-9][a-z0-9-]*)+$
```

- 已 Trim 的 ASCII 小写输入，最大 96 UTF-8 bytes。
- 严格拒绝大小写、Unicode 同形字、全角字符和自动 lowercase。
- 工具可建议 canonical 值，但不能静默写回或注册。
- 发布后不可修改、不可复用。

slug：

```regex
^[a-z0-9]+(?:-[a-z0-9]+)*$
```

- 最大 64 ASCII bytes。
- 必须等于当前功能目录名。
- Windows ordinal-ignore-case 下全仓唯一。
- 历史 slug 永久归原 FeatureId，只用于迁移、诊断和旧路径识别，不能用于当前构建加载。

### 4.2 事件文件

权威 ledger 使用不可变独立文件：

```text
registry/events/<event-id>.json
```

每个事件至少包含 eventId、eventSchemaVersion、eventType、featureId、expectedPreviousState、actor、approvalRefs、reason 和 payload。单文件模式减少并发追加冲突；Git 分支保护、CI 历史校验和审批共同提供治理不可变性，文件格式本身不声称防篡改。

CI 验证事件 ID 唯一、旧事件未修改、前置状态成立、审批满足、重放确定、投影可重建。

### 4.3 FeatureIdentityStatus

```text
Reserved → Active → Deprecated → Removed
Reserved → Abandoned
Deprecated → Active 仅通过 DeprecationRevoked
```

`Removed` 与 `Abandoned` 永久不可复用。状态由事件 projection 产生，不直接手改当前状态字段。身份状态与 `FeatureState` 完全不同；Deprecated 功能仍可在运行时 Running。

Registry 只拥有 FeatureId、current/historical slug、namespace claim reference、identity status、event references 和 tombstone/successor 导航。`dependencies/replaces/supersedes/migrationFrom` 属于 Relation module。

## 5. Feature Manifest module

`feature.json` 只声明设计意图和契约，不声明 PASS。逻辑字段族包括：

- `manifestSchemaVersion`。
- `featureId` 与 `slug`。
- 可本地化 display metadata。
- `version`（功能语义版本）。
- `contractVersion`。
- 可选 `featureApiVersion`。
- `maturityIntent`。
- 环境/角色意图。
- 核心与客户端 UI 入口声明。
- 设置、能力和关系声明引用。

首个参考实现从 `0.1.0` 开始。未完成 Stable 门禁不得使用 `1.0.0` 表示成熟。功能版本、契约版本、Manifest schema 版本和可选 SPI 版本不得复用同一字段。

Manifest、Identity Projection 与目录映射由领域 fragment compiler 裁决，随后进入 Feature-Definition-Pipeline-Spec.md 的 Linker。

## 6. Feature Relation module

关系语义集中解释：

- dependency：影响静态闭包与 Lifecycle required DAG。
- replaces：兼容替代声明，不默认表示协议兼容。
- supersedes：产品推荐替代，不触发运行时替换。
- migrationFrom：允许存在迁移实现，不自动迁移数据。

任何关系都不自动转移权限、配置、网络身份、存档或删除旧功能。关系冲突由 Relation module 输出稳定 diagnostics；Linker 只链接已裁决 relation fragment。

## 7. Build Qualification module

### 7.1 输入

- 不可变 source snapshot/commit identity。
- 已批准贡献 lineage。
- 链接功能定义包与 Definition Artifact。
- 固定工具链、SDK、编译器、NuGet/依赖 lock。
- 客户端和 U3DS reference set identity/hash。
- 聚合工程源码清单。

### 7.2 输出

`CandidateBuild` 至少绑定：

- SourceSnapshotId。
- DefinitionSetDigest。
- ArtifactPayloadDigest。
- ToolchainIdentity。
- ClientReferenceSetId 与 U3DSReferenceSetId。
- BuildIdentity。
- DLL SHA-256。
- 构建日志与静态检查结果引用。

BuildIdentity 以稳定 source/toolchain/reference identities、DefinitionSetDigest 与 ArtifactPayloadDigest 确定性计算，且不包含最终 DLL SHA-256。ArtifactPayloadDigest 使用 Feature-Definition-Pipeline-Spec.md 冻结的零值 header 槽位排除规则，避免自引用。DLL SHA-256 在最终二进制完成后计算。三者都不表示环境 PASS 或发布授权。

同一 `AssemblyBuildBinding(BuildIdentity, DefinitionSetDigest, ArtifactPayloadDigest, ArtifactFormatVersion)` 必须同时进入程序集生成注册根与定义产物 header。运行时 Build Artifact Binding Verifier 按规范把 header 中 BuildIdentity/ArtifactPayloadDigest 槽位逻辑清零后重算摘要，再验证配对；不一致进入 Core SafeMode。

### 7.3 门禁

- 领域 fragment compile、Definition Link、facet 与 Artifact Format 全部成功。
- `CoreShared` 源码闭包针对 U3DS reference set 独立编译；包含 `ClientUi` 的最终聚合 DLL 针对客户端 reference set 构建。
- 对最终 DLL 从唯一 BepInEx/Core/U3DS 入口执行可达 IL/type-reference 审计，禁止核心可达签名、基类、attribute、静态初始化和注册表引用 UI 专属类型。
- 禁止 `Assembly.GetTypes()`、全局 `PatchAll()` 和入口扫描；核心与 ClientUi 使用生成的显式注册 section，后者只在客户端门禁后解析。
- 唯一 `[BepInPlugin]` 与单 DLL 输出。
- 禁止未声明入口与程序集扫描。
- 单元、契约、序列化、生命周期、设置、网络与算法测试。
- 两次干净 checkout、固定工具链构建产生相同候选 DLL bytes/SHA-256；若 PDB/路径映射另有非决定字段，必须显式剥离或规范化，不能把差异忽略为成功。

输出只表示 `CandidateBuildProduced` 或构建失败，不表示 ReleaseReady。

## 8. Qualification Obligation Derivation

所有框架和内置功能必须面对单人、SteamP2PFriends 和 U3DS 产品级矩阵。功能环境声明不能选择退出某个环境；它只能表达 facet 适用角色。例如 UI 在 U3DS 为 NotApplicable，但功能仍需证明 headless 核心安全加载。

义务从规范定义和项目政策推导，包括：

- 构建/静态义务。
- 单人运行义务。
- SteamP2PFriends Host/Client 双端义务。
- U3DS headless 运行义务。
- UI 防穿透义务。
- 故障隔离义务。
- 同候选 DLL 身份义务。

义务不读取日志、不判 PASS。

## 9. Evidence Case & Provenance module

每个证据案例绑定：

- CaseId。
- CandidateBuild/BuildIdentity。
- DLL SHA-256。
- DefinitionSetDigest 与 ArtifactPayloadDigest。
- 场景、角色、机器/环境指纹。
- 游戏、BepInEx、LMN、SteamP2PFriends/U3DS 版本。
- 引用集或部署路径来源。
- 命令、步骤、UTC/本地时间窗口。
- 原始日志、截图或录像引用。
- 采集者与诊断摘要。

SteamP2PFriends 必须使用同一 CaseId 的 Host 与 Client 两端案例，且两端确认同一 DLL SHA-256。缺任一端、hash 不同或时间窗口无法关联时不能满足 P2P 义务。

任何 DLL/source 变化产生新候选 hash 后，旧运行证据不得继承；Qualification Evaluation 将其标记为 Stale/Mismatched。

## 10. Qualification Evaluation module

技术 verdict：

```text
Fulfilled
Failed
Missing
Stale
NotApplicable
```

Evaluation 只比较一组资格义务与同一 CandidateBuild 的证据案例：

- SP 不能替代 P2P 或 U3DS。
- 静态 U3DS 防穿透不能替代真实 U3DS 启动/运行。
- Build success、hash 或 patch 注册不能替代功能运行。
- 人工说明不能补齐缺失原始证据。
- 一个功能的环境 PASS 不能自动证明其他功能或框架整体 PASS，除非义务明确属于共享核心且证据覆盖该事实。

技术 verdict 不修改 maturity intent、FeatureState 或发布状态。

## 11. Release Claim & Authorization

正式发布或 maturity 提升要求：

- Contribution Governance lineage 完整。
- CandidateBuild 身份唯一且构建资格通过。
- 对当前候选 hash 的所有 required 技术义务为 Fulfilled；允许的 NotApplicable 必须来自资格政策，不由作者自行声明。
- 无 Failed/Missing/Stale。
- 共享契约变更完成 contract maintainer 与 frontend reviewer 复核。
- human release approver 明确批准具体 BuildIdentity 与 DLL SHA-256。

发布冻结条件包括 hash 不匹配、证据陈旧、场景缺失、审批缺失、来源无法追踪或共享契约复核未完成。Release module 不执行编译、重跑测试或重新解释日志。

## 12. Runtime Feature Admission module

Runtime Admission 位于 Artifact 验证与 GPT-09 生命周期 `Starting` 之前。它消费：

- 已完整验证的单一定义产物。
- 当前进程角色和可用环境能力。
- Admission/Lifecycle facet。
- 已链接依赖状态与入口绑定。

它不消费 Release Evidence；开发构建和正式构建使用相同运行准入规则。

一次全仓评估输出一个判别联合：

- `CoreEscalation`：定义产物缺失、格式不支持、全局摘要/section 索引损坏、Definition Set 不一致或无法确定影响范围。此分支不包含 per-feature decisions，ModuleRuntime 不得从不可信产物建立 `Discovered` 记录，核心直接进入 SafeMode。
- `TrustedFeatureDecisions`：仅在产物全局可信时包含覆盖定义集中全部功能的不可变 decisions：
  - `Admit(handle)`：静态身份、入口、定义版本和当前进程环境准入成功；是否启用不在 Admission 裁定；
  - `RejectIncompatible(reason)`：身份、入口、静态版本或环境不兼容，不产生 handle，当前进程不可启用。

admitted feature handle 内部绑定当前验证产物、FeatureId、入口与 feature-scoped bootstrap factory，交给 Lifecycle；它不是可序列化权限令牌，也不证明代码安全、运行成功或发布资格。

Admission 吸收原先 shallow 的 Integrity → Identity Gate → Permit → Loader 前置判断。可信分支以不可变 `AdmissionEvaluationBatch` 返回完整 decisions；ModuleRuntime/Lifecycle 是 `FeatureState`、`StateRevision` 和 `FeatureStatusChangedEvent` 的唯一写入者：它先为可信批次中的全部功能建立 `Discovered`，保存 `Admit(handle)` 的 handle，并把 `RejectIncompatible` 转入 `Incompatible`。随后 SettingsRuntime 按静态 Settings facet 完成迁移、校验和只读 enablement snapshot；ModuleRuntime 结合该快照、当前动态依赖与政策，唯一执行 `Discovered → Starting/Disabled`。Admission 不读取配置文件、不裁定用户启用状态，也不直接写生命周期状态。

## 13. CI Gate Matrix

| 触发 | 必须执行 | 可以阻断 |
| --- | --- | --- |
| Pull Request | governance、schema、fragment compile、link、artifact、tests、双引用集静态检查 | 合入 |
| main | 全部 PR gates、干净确定性构建、CandidateBuild 归档 | 候选构建产生 |
| manual runtime evidence | 证据案例 schema、hash/CaseId/角色绑定、Qualification Evaluation | 技术资格更新 |
| release candidate | 当前候选全部资格、共享契约复核、人工授权 | Release/maturity 提升 |

CI workflow 只编排这些 module 并呈现 diagnostics；规则不得复制进 YAML。

## 14. 开放贡献默认策略

- 新功能初始 `Experimental` 且默认关闭。
- 完成同一候选 DLL hash 的三环境技术资格后，才能申请 Stable/default-on。
- 合入后原作者为主要 CODEOWNER；项目维护角色保留兼容修复、紧急隔离和弃用权。
- 正式移除经过弃用周期；安全漏洞、核心污染或许可证问题可紧急冻结，但仍保留身份墓碑与审计记录。

## 15. 证据边界

本规范仍处于 Wayfinder。仓库当前没有生产工程或 DLL；Markdown、HTML 审查、schema 草案和独立子智能体 PASS 都不构成 Build Qualification 或任何环境 PASS。

