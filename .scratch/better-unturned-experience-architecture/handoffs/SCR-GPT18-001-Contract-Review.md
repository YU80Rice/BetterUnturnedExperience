> 接管说明：该文档原由 Gemini 负责，现由 GPT 接手维护。

# Gemini-SCR-GPT18-001-Contract-Review：SCR-GPT18-001 外部功能注册与 LoadSet 契约提案复核报告

> **作者**: Gemini（前端负责人 / 表现层与消费端负责人）  
> **接收方**: GPT（后端负责人、架构与发布门禁） / 人工开发者  
> **复核方法论**: `diagnosing-bugs`（边界不变性审计、时序与状态机断言、零类型泄漏验证） + `codebase-design`（深度模块 Seam、单一事实源、平权架构）  
> **复核对象**:  
> 1. 契约提案：[`SCR-GPT18-001-Contract-Proposal.md`](../SCR-GPT18-001-Contract-Proposal.md)  
> 2. 工单：[`issues/SCR-GPT18-001-registration-loadset-contract.md`](../issues/SCR-GPT18-001-registration-loadset-contract.md)  
> 3. 复核请求：[`handoffs/to-SCR-GPT18-001-review.md`](../handoffs/to-SCR-GPT18-001-review.md)  
> **契约基线**: `RT-01-Shared-Contract-Baseline.md` (`BUE-V1-RT01-20260824`)  
> **SourceSet 身份**: `BUE-SS-20260824-02`  
> **判定结论**: **ACCEPT（全量通过，无阻断异议，无契约冲突，正式签署同意 SCR-GPT18-001 契约提案）**  

---

## 一、 核心复核结论与判定矩阵

| 审查维度 | 判定结果 | 前端/表现层核查说明 |
| :--- | :---: | :--- |
| **1. 最小注册公开 Interface** | **`ACCEPT`** | `IBueFeatureRegistrationHost`、`IFeatureRegistration`、`IFeatureModuleFactory` 与 `IClientUiSatelliteRegistration` 接口极简，**对 Unity/Glazier/Sleek/LMN/BepInEx 类型的引用数精确为 0**。 |
| **2. 四阶段注册确定性时序** | **`ACCEPT`** | `HostStarting → RegistrationOpen → CatalogFrozen → RuntimeReady` 时序清晰；晚注册（`PhaseClosed`）与重复注册（`DuplicateFeature`）提供结构化原因码与 `DiagnosticId`，前端可安全投影至未准入列表。 |
| **3. UI 卫星与表现状态投影** | **`ACCEPT`** | `IClientUiSatelliteRegistration` 仅包含纯值元数据；当卫星缺失或失败时优雅降级为 `PresentationDegraded` / `HeadlessOnly`，**BUE 统一设置中心仍可稳定加载并编辑该功能的 Settings Facet**。 |
| **4. 官方功能绝对平权** | **`ACCEPT`** | 内置 Better Item Interaction 100% 走同一个 `IFeatureRegistration` 与生命周期装配 Seam，彻底杜绝隐藏特权路径，前端 Presenter 无需维护官方/第三方分支。 |
| **5. FeaturePresentationState 契约化** | **`ACCEPT`** | 同意将 `FeaturePresentationState` 与 `FeaturePresentationView` 作为只读投影类型纳入 `ContractTypes.cs`，实现运行状态（`FeatureState` 9 态）与表现状态的解耦并存。 |
| **6. SDK、LoadSet 与 U3DS 证据边界** | **`ACCEPT`** | SDK 保持 compile-time-only；U3DS 部署 profile 排除 UI 卫星；`LoadSetIdentity` 严格绑定实际部署全量程序集哈希，Preflight 与 Runtime Isolation 职责划分清晰。 |

---

## 二、 针对复核请求 6 项问题的逐项详细裁定

### 1. 最小公开 Interface 完备性与零类型泄漏
* **裁定：完全接受（ACCEPT）。**  
  经审查提案 §4.2：
  * `FeatureRegistrationPhase`、`FeatureRegistrationReason`（11 项枚举值）、`FeatureDefinitionArtifact`、`IFeatureModuleFactory`、`IClientUiSatelliteRegistration`、`IFeatureRegistration`、`IBueFeatureRegistrationHost` 与 `FeatureRegistrationResult` 均为纯 C# 值类型/接口。
  * 外部功能入口只需提交不可变的 `IFeatureRegistration`，所有 Catalog 排序、Schema 校验、权限控制与生命周期绑定由 BUE 内部完成，深度模块杠杆（Leverage）极高。

### 2. 注册时序与错误安全投影
* **裁定：完全确认（CONFIRMED）。**  
  * 外部插件在 BepInEx `Awake` 期间通过 `RegistrationOpen` 提交注册；
  * BUE 收集完毕后在 `CatalogFrozen` 进行 Ordinal 排序与规范化，生成确定性的 `CatalogRevision`；
  * 进入 `RuntimeReady` 统一启动功能；
  * 任何晚注册（`CatalogFrozen` 后）、重复 FeatureId 或依赖缺失均以明确的 `FeatureRegistrationReason` 被拒绝，前端管理界面可直观呈现被拒原因而不暴露内部异常堆栈。

### 3. 卫星缺失时的表现降级与设置主权
* **裁定：完全接受（ACCEPT）。**  
  * 当客户端检测到 Satellite DLL 缺失、版本不兼容或初始化失败时，前端将 `FeaturePresentationState` 标记为 `PresentationDegraded` 或 `HeadlessOnly`；
  * **主权保持**：BUE 统一设置模态窗（`SleekFeatureSettingsModal`）消费 Core DLL 编译期生成的 Settings Facet，第三方插件不能向设置窗口注入原始控件；即使 UI 卫星缺失，设置项依然 100% 可用。

### 4. 官方功能与第三方功能平权
* **裁定：完全确认（CONFIRMED）。**  
  * 官方功能 Better Item Interaction 与第三方功能在注册、生命周期、设置快照和隔离规则上完全一致，前端无需感知功能的部署物理形态（内置或独立 DLL）。

### 5. `FeaturePresentationState` 共享契约纳入
* **裁定：完全赞同（CONFIRMED）。**  
  * `FeaturePresentationState`（`NotApplicable = 0, Available = 1, PresentationDegraded = 2, HeadlessOnly = 3, Failed = 4`）与 `FeaturePresentationView` 作为纯值状态视图纳入 `ContractTypes.cs`，能让前端 Presenter、HUD 挂载器与设置面板拥有统一的契约定义。

### 6. SDK 模型、LoadSetIdentity 与证据边界
* **裁定：完全接受（ACCEPT）。**  
  * SDK 仅为开发期依赖，无需作为独立运行时部署；
  * `LoadSetIdentity` 包含 BUE Host、功能 DLL、UI 卫星与 reference-set 的全量 SHA-256 摘要，彻底杜绝拼接单端证据；
  * Preflight 门禁在构建/加载前拦截静态风险，Runtime Isolation 在运行时隔离异常。

---

## 三、 结论与后续推进

1. **提案状态确认**：Gemini 正式签署并批准 `SCR-GPT18-001` 契约提案。
2. **约束维持**：
   * 本复核仅代表**共享契约设计方案的定稿批准**，本轮未修改 `ContractTypes.cs`，未破坏 DEV-01～DEV-08 的已验收成果。
   * 下一步可在人工开发者批准后，立项开启具体的 DEV 工单（如契约扩展实现、Definition Artifact 桥接与 No-op 外部功能夹具验证）。

---

*报告完。作者: Gemini*



