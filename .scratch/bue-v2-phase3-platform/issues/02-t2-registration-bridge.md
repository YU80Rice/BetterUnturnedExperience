# V3-T2 公开注册桥、Registration Admission 与 Bootstrap composition

- **Ticket**: V3-T2
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-09 七项裁决定音，方案 A）
- **Blocked By**: V3-T1, V3-R1
- **Map**: [map.md](../map.md)

## Question

`BueRuntimeHost.Register`、`FeatureRegistrationRuntime`、`FeatureBootstrap`、`BueFeatureStartRuntime` 共同组成同一个生态接入 seam——合并一票裁决（2026-09-09 用户裁定：拆成多张早期票会重复讨论注册事实、运行时 scope 与能力注入）。风险级 **Strong**。按六问结构：

1. **注册接纳（Admission）**：FeatureId 校验、合同版本检查、防重复注册、可用性判断的判断链——哪些进公开契约、哪些留内部？拒绝路径的失败语义（显式结果 / 异常 / 结构化诊断条目）？
2. **Bootstrap composition**：哪些平台服务在 `IFeatureBootstrap` 上注入（Network / Events / Settings / Clock / Diagnostics…）、注入时机与不可变性承诺（`Network` 永非 null 先例是否推广到全部服务）？
3. **实施深度**：本阶段做到哪层（现状加固 vs 重构 admission 管线 vs 引入 registration session 生命周期）？
4. **留到后续**：哪些明确不做（对照图级冻结）？
5. **契约影响**：是否升契约版本？SCR-GPT18-001 公开桥措辞是否修订？
6. **同权影响**：官方功能走同一注册链还是内部直通？同权的检验点在哪？

事实输入：V3-R1 现状面盘点。主源：SCR-GPT18-001、SDK 契约文档 §4、NoOpFixture 活样板（`src/BetterUnturnedExperience.NoOpFixture/NoOpFeaturePlugin.cs`）。

## Answer

2026-09-09 用户+PM 七项裁决全部定音，**方案 A（现状加固）通过**。本票交付=把注册桥从「可调用的转发 seam」加固成「规则明确、结果可分支、官方与生态同入口、契约可验证」的 admission module。

### 1. 保留段拒绝 = 官方身份白名单（方案 A）

- `io.github.yu80rice.bue.*` 为官方保留段；保留段内 FeatureId 仅当 **∈ BUE 官方身份白名单**（BII/LIT/LIR/LHT/BUE Network/ClientUi satellite 及其他随主 DLL 正式发布且已登记的官方身份）才可注册。保留段内而不在白名单：`Accepted=false, Reason=ReservedFeatureId, DiagnosticId=BUE-REG-010`。
- 判定顺序冻结：①FeatureDefinition 基础校验 → ②FeatureId 格式校验 → ③官方保留段校验 → ④合同版本校验 → ⑤重复 FeatureId 校验 → ⑥接受/拒绝。
- 生态即使用官方白名单身份也不能获得官方资格（由重复/身份冲突规则拒绝）；**不反射 caller assembly、不读文件路径判官方来源**；白名单=主 DLL 内确定性官方身份事实源，不接受生态自报。

### 2. Admission 三类型整体入 SDK 冻结面

`FeatureRegistrationResult` / `FeatureRegistrationPhase` / `FeatureRegistrationReason` 作为一个整体登记（生态要按 Reason 分支：继续运行/等待/禁用自身/提示缺前置/报版本不兼容）。版本规则：现有 Reason 增 `ReservedFeatureId`=Minor；`BUE-REG-001..010` 诊断码登记进 SDK 码表附录；枚举已有值语义不可静默改变；新增枚举值必须保持旧模块可解析可降级；拒绝路径显式结果不抛异常；Phase 状态转换语义同步登记。SDK 须列出 Phase/Reason/Accepted/Feature/DiagnosticId 及每种拒绝的开发者处理建议。

### 3. Bootstrap 成员三层承诺

- **第一层（永非 null，入冻结面+红测）**：`Identity` / `LifecycleGeneration` / `Events` / `OwnedEvents` / `Network`。红测双写：官方模块启动五成员非 null；生态 NoOp/Contract Probe 启动五成员非 null。
- **第二层（本阶段恒 null，语义冻结）**：`Settings` / `Logger` / `Dependencies` / `Lifetime`——保留接口成员，SDK 文档登记「本阶段不可用」，生态不得使用；红测断言恒 null；不得静默接线不升版本；**null 是当前阶段的冻结语义，不是"暂时忘了实现"**。
- **第三层（后续归属）**：Lifetime→V3-T4；Dependencies→V3-T4（不建第二套依赖求解器）；Settings→V3-T7；Logger→V3-T8。不删除成员（删=Major 破坏且没必要）。

### 4. 实施深度 = 现状加固三件套

①保留段检查（白名单+ReservedFeatureId+BUE-REG-010）；②admission 全链逐码红测锚（BUE-REG-001..010 每码至少一锚，验 Accepted/FeatureId/Reason/DiagnosticId 四元组+拒绝不抛异常；重点含 HostStarting/CoreSafeMode/PhaseClosed/InvalidDefinition/InvalidModuleFactory/ContractIncompatible/DuplicateFeature/InvalidClientUiRegistration）；③契约登记（SDK 文档：三类型+码表+成员承诺表+保留段合法/非法示例+T1 契约面定义+同权检验规则）。**不做**：registration session、owner token、外部模块加载器、依赖图求解、生命周期全面重构、四 null 成员实际注入、重设计 Phase 状态机——现有注册阶段状态机是当前有效的深 module，加固其 interface 与 test surface 而非套第二层概念。

### 5. 留到后续（照单全收）

Dependencies 求解（BepInEx 继续负责插件依赖发现/排序）；Logger→T8；Settings→T7；Lifetime/Dependencies 注入→T4；registration session 仅当 T4/T10 重新证明现有 owner/lifecycle seam 不足才重开。不偷偷塞进 T2 的：Tick scheduler、Network lease、Contracts.dll 拆分、自动诊断包采集、官方功能全改公开服务、生态模块自动扫描。

### 6. 契约影响 = Minor，2.0 → 2.1

两笔加性变更：①`ReservedFeatureId`+拒绝语义；②Bootstrap 五成员永非 null 承诺。宿主门槛 `SupportedContractMajor=2` / `SupportedContractMinor=1`；2.0 模块继续可注册但不依赖 2.1 新增保证，2.1 模块可依赖全部。SDK 修订清单：三类型及成员、码表 001..010、成员承诺表、T1 契约面定义、保留段、合法/非法生态命名示例、2.0→2.1 迁移说明、RELEASES 版本注记规则。恒 null 语义写入 SDK 后，未来改为可用必须由对应阶梯票产生新 Minor。

### 7. 同权检验（三+一条）

①注册入口同权：官方/生态同走 `BueRuntimeHost.Register`；②保留段双向红测：官方白名单身份→接受、生态冒用保留段→ReservedFeatureId 拒绝（证明规则对所有注册请求同一执行）；③无官方专用绕行：官方注册仍走公开 Register、不直接调 FeatureRegistrationRuntime 宿主控制方法、Bind/Clear/宿主阶段控制面保持 internal、生态不可调用、官方也不绕过公开 admission（红测或静态检查确认）；④官方同样受重复与契约检查：白名单只解决保留段使用权，不授予绕过其他 admission 规则的特权。

### 交付 / 不交付

交付：保留段白名单+ReservedFeatureId 拒绝；三类型+十诊断码入冻结面；五成员永非 null+四成员恒 null 分层承诺；契约 2.0→2.1；admission 全链逐码红测；同权四条检验。不交付：registration session、第二套依赖求解器、Settings/Logger/Dependencies/Lifetime 实现、Contracts.dll 拆分、扫描器/加载器、T4/T7/T8 的 implementation 提前搬入。

## Comments
