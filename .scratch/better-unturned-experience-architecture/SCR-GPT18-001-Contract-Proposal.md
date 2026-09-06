# GPT-SCR-GPT18-001：外部功能注册、BUE Host 与 LoadSet 契约提案

Status: proposed-for-dual-review
Owner: GPT
SourceSet: BUE-SS-20260824-02
Baseline: BUE-V1-RT01-20260824
Related issue: `issues/SCR-GPT18-001-registration-loadset-contract.md`

## 1. 变更目的

将 BUE 从“只能把功能源码聚合进一个 DLL”扩展为“运行在 BepInEx 之上的前置框架插件 + 独立外部功能 DLL”，同时保留 Contracts、Definition Artifact、Runtime Admission、生命周期、设置、UI 和证据事实源的单一所有权。

本提案只冻结注册、装配、物理 Host、UI satellite 和证据身份；不实现第三方功能，不修改 LMN，不修改 DEV-01～DEV-09 已验收行为。

## 2. 术语与物理部署模型

### 2.1 三层装载关系

```text
BepInEx
  └── BetterUnturnedExperience.dll
        ├── public BUE ABI / Contracts namespace
        ├── Core / Definition / Admission / Lifecycle / Settings runtime
        ├── official feature registrations
        └── BUE-owned unified management projection

Third-party Feature DLL
  ├── independent BepInEx entry
  ├── BepInEx dependency on BUE plugin id
  ├── generated immutable definition artifact
  └── explicit BUE registration call

Optional ClientUi satellite
  ├── separate client-only deployment asset
  └── excluded from U3DS deployment profile
```

### 2.2 物理程序集规则

- BUE 用户运行时的公开 ABI、Contracts 和 Core implementation 必须由一个明确的 BUE Host deployment assembly 提供；其 assembly identity 和 public namespace 在发布前冻结。
- 开发期可以继续保留 `Contracts`、`Core`、`ClientUi`、`Transport` 等多项目源码与测试工程；这些是源码模块和内部 Seam，不是玩家必须手动补齐的隐式运行时依赖。
- 第三方功能运行时只依赖 BUE Host 的 public ABI；SDK 不作为运行时程序集安装。
- BUE Host 的官方功能可以物理内置，但必须通过本提案规定的同一个注册入口和生命周期路径注册，不得使用私有特权路径。
- BUE Host 自身的 UI 类型仍必须经过 CoreShared/ClientUi 可达性、IL/type-token 和真实 U3DS 装载门禁；`Application.isBatchMode` 不能单独证明安全。

## 3. BUE 依赖身份与版本

- BepInEx plugin id：`io.github.yu80rice.betterunturnedexperience`。
- 每个外部功能 BepInEx entry 必须声明对该 plugin id 的依赖；最低 BUE plugin version 与最低 BUE `ContractVersion` 分别记录。
- BepInEx plugin version 负责底层装载依赖排序；`ContractVersion` 负责公开注册 Interface 兼容性；Feature semantic version 负责功能自身兼容性，三者不混用。
- BUE 当前仍为预发布版本；本 SCCR 不把 `0.0.0` 升级为 Stable 或 `1.0.0`。

## 4. 注册外部 Seam

### 4.1 设计原则

注册 Seam 必须是一个窄的深模块 Interface：外部入口只提交一个不可变注册包，BUE 内部负责验证、规范化、Catalog 组合、Admission 和生命周期绑定。外部功能不得提交原始 manifest、任意 FeatureId 查询、UI root、Unity/Glazier/Sleek/LMN 对象或可变全局状态。

### 4.2 候选公开契约

下列类型是本 SCCR 的候选共享契约，须经 Gemini 复核后才能写入 `ContractTypes.cs`：

```csharp
public enum FeatureRegistrationPhase : byte
{
    HostStarting = 0,
    RegistrationOpen = 1,
    CatalogFrozen = 2,
    RuntimeReady = 3,
    CoreSafeMode = 4
}

public enum FeatureRegistrationReason : ushort
{
    None = 0,
    HostUnavailable = 100,
    PhaseClosed = 101,
    DuplicateFeature = 200,
    InvalidDefinitionArtifact = 201,
    ContractIncompatible = 202,
    MissingRequiredDependency = 203,
    InvalidModuleFactory = 204,
    InvalidClientUiRegistration = 205,
    PreflightRejected = 300,
    CoreUnavailable = 900
}

public sealed class FeatureDefinitionArtifact
{
    public ushort FormatVersion { get; }
    public string DefinitionSetId { get; }
    public Digest256 DefinitionSetDigest { get; }
    public Digest256 ArtifactPayloadDigest { get; }
    public IReadOnlyList<byte> CanonicalPayload { get; }
}

public interface IFeatureModuleFactory
{
    IFeatureModule Create();
}

public interface IClientUiSatelliteRegistration
{
    string SatelliteId { get; }
    ContractVersion MinimumBueContract { get; }
    string RegistrationToken { get; }
}

public interface IFeatureRegistration
{
    FeatureDefinitionArtifact Definition { get; }
    ContractVersion MinimumBueContract { get; }
    IFeatureModuleFactory ModuleFactory { get; }
    IClientUiSatelliteRegistration ClientUi { get; }
}

public interface IBueFeatureRegistrationHost
{
    FeatureRegistrationPhase Phase { get; }
    FeatureRegistrationResult Register(IFeatureRegistration registration);
}

public readonly struct FeatureRegistrationResult
{
    public bool Accepted { get; }
    public FeatureId Feature { get; }
    public FeatureRegistrationReason Reason { get; }
    public string DiagnosticId { get; }
}
```

### 4.3 注册包不变量

- `FeatureDefinitionArtifact` 是生成的不可变定义事实；它包含 format version、DefinitionSet identity、DefinitionSet digest、ArtifactPayload digest 和规范化 payload。它不包含可执行委托、程序集扫描结果或运行时状态。
- `IFeatureModuleFactory` 是运行时可执行输入，只在 BUE 已验证 Registration 后保存；它不是 Definition Artifact 的序列化字段，也不产生第二身份事实源。
- `ClientUi` 可以为空。若存在，它只携带无 UI 类型泄漏的 satellite identity、contract range、能力标识和生成 registration token；具体 Glazier/Sleek/Unity component interface 不在本 SCCR 中公开冻结，须由独立 ClientUi contract change 决定。
- 注册调用者是外部功能自己的生成 BepInEx entry；BUE 不扫描外部程序集。BUE Host 的官方功能使用同一个 `IBueFeatureRegistrationHost`。
- `Register` 不接受任意 FeatureId 路由参数；FeatureId 必须来自 Definition Artifact 并由 BUE 校验。
- 同一 BUE Host lifecycle generation 内，同一 FeatureId 只能成功注册一次；重复注册 fail-closed。
- 注册返回稳定 reason 和 DiagnosticId，不把异常堆栈或内部类型暴露给前端。
- `FeatureRegistrationResult` 不携带按到达顺序递增的 revision；BepInEx entry 到达顺序不得改变结果。最终 `CatalogRevision` 只在全部注册规范化、按 FeatureId/digest 排序并冻结后生成。

### 4.4 注册调用时序

```text
HostStarting
    ↓ BUE Host Awake 完成公开 runtime 初始化
RegistrationOpen
    ↓ 外部/官方 BepInEx entries 显式 Register
CatalogFrozen
    ↓ 全部 registration 验证、确定性排序、Definition/Catalog projection 完成
RuntimeReady
    ↓ Admission + Settings bootstrap + Lifecycle 统一 Start
Feature module Start/Running
```

- 只有 `RegistrationOpen` 接受注册。
- `HostStarting` 调用返回 `HostUnavailable`，不得绕过 BepInEx dependency 顺序；不得创建未绑定 runtime 的 feature instance。
- `CatalogFrozen`、`RuntimeReady` 和 `CoreSafeMode` 的晚注册都返回 `PhaseClosed` 或 `CoreUnavailable`，要求重启。
- 外部功能的 BepInEx `Awake` 只注册，不直接调用 `IFeatureModule.Start`；Start 由 BUE 在 Admission、Settings bootstrap 和 dependency validation 后执行。
- 注册过程在 BUE 规定的初始化线程执行；外部网络/UI callback 不得调用注册入口。
- Catalog 冻结后 identity 事实不可变；对外投影使用由 canonical registration set 计算出的 `CatalogRevision`，不使用插件到达顺序。

## 5. ClientUi satellite 与表现降级

### 5.1 部署

- ClientUi satellite 是独立客户端部署资产；U3DS deployment profile 默认不包含它。
- Core feature DLL 不得引用 Glazier、Sleek、Unity UI 或其他客户端专属程序集。
- BUE 只在客户端能力成立且 UI satellite registration token 有效时解析/实例化表现层。
- 静态 IL/type-token 扫描和 U3DS 实际装载验证是独立门禁；runtime batchmode 是必要运行条件而非结构证明。

### 5.2 表现状态

不把表现状态混入 `FeatureState`。候选新增只读投影：

```csharp
public enum FeaturePresentationState : byte
{
    NotApplicable = 0,
    Available = 1,
    PresentationDegraded = 2,
    HeadlessOnly = 3,
    Failed = 4
}

public readonly struct FeaturePresentationView
{
    public FeatureId Feature { get; }
    public FeaturePresentationState State { get; }
    public string DiagnosticId { get; }
    public ulong PresentationRevision { get; }
}
```

- Core 成功、satellite 缺失：`PresentationDegraded` 或 `HeadlessOnly`，具体映射由 SCCR 复核冻结。
- Settings Facet/Snapshot 仍由 BUE 统一设置中心渲染和编辑。
- BUE 设置模态窗拥有原生 UI 树主权；外部功能不得注入原始 Sleek/Unity 控件。
- 自定义 HUD/玩法视觉表现必须通过后续 ClientUi registration/component contract；本 SCCR 只冻结无 UI token 泄漏的 satellite registration 元数据。

`IClientUiSatelliteRegistration` 在本 SCCR 中只能表示纯值元数据和生成 token，不允许暴露 `Glazier`、`Sleek`、`UnityEngine`、原生 UI root 或可执行 UI factory。`RegistrationToken` 只是由 BUE 校验的生成查找标识，不是权限或 capability；完整 `IClientUiFeatureComponent` 属于后续 ClientUi contract change。

## 6. 官方功能平权

- Better Item Interaction 使用与 no-op 第三方功能相同的 `IFeatureRegistration`、`IFeatureModuleFactory`、Settings Facet、Lifecycle 和 isolation Seam。
- BUE Host 可以物理内置官方功能，但只允许通过同一公开注册 Host 完成登记；不得调用 `RegisterOfficialFeature` 等隐藏特权入口。
- 官方功能与第三方功能均生成 Definition Artifact、FeatureId、Settings Facet 和 Candidate/LoadSet evidence。

## 7. LoadSetIdentity 与证据绑定

`LoadSetIdentity` 属于 Release/Qualification module 的不可变事实，不是模块运行时权限：

```text
LoadSetIdentity = canonical digest of
  BUE Host assembly identity + SHA-256
  ordered feature DLL identities + SHA-256
  ordered ClientUi satellite identities + SHA-256 (or explicit absent marker)
  DefinitionSetDigest
  ArtifactPayloadDigest
  ClientReferenceSetId
  U3dsReferenceSetId
  ToolchainIdentity
```

- 文件路径使用规范化相对路径；列表按稳定 Ordinal 顺序排序；摘要使用长度前缀 UTF-8 canonical serialization。
- BUE Host、功能 DLL、satellite、Definition Artifact 和 reference-set 任一变化都会生成新的 LoadSetIdentity。
- SP、SteamP2PFriends Host/Client、U3DS 的 Evidence Case 必须绑定完全相同的 LoadSetIdentity；不能用单个 BUE DLL hash 或单个 feature hash 拼接证据。
- `BuildIdentity` 仍可表达 source/toolchain/reference/definition 的确定性身份；`LoadSetIdentity` 表达实际部署程序集集合；二者不混为一谈。

## 8. Preflight 与故障边界

- BUE 局部隔离从“BepInEx/CLR 成功加载、显式注册完成并进入 Lifecycle Seam”开始。
- Chainloader、程序集绑定、类型解析、静态初始化失败无法由 BUE runtime isolation 捕获；SDK/CI preflight 必须在安装/候选构建前检查目标框架、引用闭包、BUE API range、UI token、依赖图和可静态识别的静态 initializer 外部引用。
- preflight 失败返回 `PreflightRejected`/构建诊断，不投影为 `FeatureState.Isolated`。
- preflight 只能拒绝可静态识别的风险，不能证明任意第三方静态初始化逻辑安全。通过 preflight 也不证明玩法正确、网络正确或三环境运行通过。

## 9. 兼容性与证据验收

### 共享契约验收

- Contracts/Core/Release 仍不得引用 Unity、Glazier、Sleek、LMN、Unturned 或 BepInEx 具体类型。
- 新增 registration/presentation 类型必须是纯 .NET Framework 4.7.2/C# 10 兼容表面。
- 稳定错误、阶段、presentation state 和 registration result 必须显式枚举值并可本地化。
- 删除 registration field、改变 phase 或改变 LoadSet canonicalization 必须触发主版本/显式 SCR。

### 行为与部署验收

- 干净 plugins 目录只放 BUE Host 和 no-op 外部功能 DLL 时，BepInEx 成功加载且无未声明 Contracts/Core runtime dependency。
- 打乱外部插件加载顺序，registration/canonical catalog/`CatalogRevision` 结果一致。
- `CatalogFrozen` 后注册被拒绝；重复 FeatureId、错误 artifact、错误 API range、缺依赖和坏 satellite 不污染其他功能。
- U3DS profile 不部署 ClientUi satellite；静态 token 门禁和真实 U3DS load 分别通过。
- 缺 satellite 时 Settings Facet 仍可用，presentation state 稳定降级。
- 官方 Better Item Interaction 与 no-op 外部功能的注册、生命周期和隔离调用路径一致。
- 混合 BUE/feature/satellite hash 的证据被 LoadSetIdentity 拒绝。

## 10. 变更影响与暂不修改范围

- 本提案会影响 `ContractTypes.cs`、BUE Host 聚合工程、Definition Artifact reader/registration bridge、Release CandidateBuild/Evidence schema 和 ClientUi registry；这些改动必须在 SCCR 获得 Gemini/人工批准后另立 DEV 工单实施。
- 本轮不修改 `ContractTypes.cs`，不重写 DEV-01～DEV-09，不构建 no-op fixture，不修改 LMN，不宣称运行或发布通过。
- `FeaturePresentationState`、`IClientUiFeatureComponent` 的完整 UI 操作接口和实际 BUE Host source aggregation 可在本 SCCR 后拆成实现票，但不能绕过本提案的公开事实与部署规则。
