# V3-T9 生态 SDK、契约文档与 Contracts 拆分重评

- **Ticket**: V3-T9
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-10 七项裁决定音，方案 A）
- **Blocked By**: V3-T1, V3-R1
- **Map**: [map.md](../map.md)

## Question

阶梯四级（SDK/开发者契约）+ 具名子问题 **Contracts 拆分四条件重评**（对账残差落位，2026-09-09 用户裁定）。建议在 T2..T8 核心契约裁决之后开工（PM 排序：SDK 放在核心契约稳定之后），不设硬阻塞。

1. **SDK 形态**（PM 悬案）：SDK 是什么——文档（现有 `docs/sdk` 唯一事实源的扩写）、模板（NoOpFixture 之外的项目模板）、验证工具（生态 DLL 验证门禁：引用面/CopyLocal/禁捆绑/双装检查的自动化核验）、还是三者组合？本阶段做到哪层？
2. **具名子问题：Contracts 拆分四条件重评**——T7 决策 2 冻结的四条件（①第三方需脱离完整 BUE DLL 编译 ②多仓库需稳定纯契约包 ③runtime 与 SDK 发布节奏须独立 ④需公开桥接 adapter 而不暴露主程序集）逐条对照 V3-R1 事实基线重评：四条件现状 + 触发预判 + 门禁措辞更新。拆分实施动作不进本图（图级冻结）。
3. **开发者契约文档演进**：DEV-V2-23 八节文档在本阶段要补哪些节（平台服务清单/生命周期/诊断/设置——随 T2..T8 结论）；
4. **示例与验收**：生态 DLL 验证门禁的验收形态（NoOpFixture 之外要不要第二个样板？U3DS 安全样板？）；
5. **契约影响**：SDK 交付物本身的版本纪律；
6. **同权影响**：SDK 面向生态，官方功能是否也走同一 SDK 路径自我验收。

事实输入：V3-R1（四条件事实基线 + 真实第三方需求）。主源：T7 结单决策 2（四条件原文）、`docs/sdk` 契约文档、愿景产品形态节。

## Answer

2026-09-10 用户+PM 七项裁决全部定音，**方案 A（文档为主）通过**。deepening 目标=把分散在 T1..T8 的开发者承诺深化为由正文+附录+活样板+自检清单组成的单一 SDK 契约 module。

### 1. SDK 形态 = 文档为主，模板与工具不建（方案 A）

本阶段 SDK 主交付=开发者契约文档+活样板 NoOpFixture+已验证接入路径。**不建设**：独立项目模板、编译期验证 CLI、自动打包工具、独立 SDK NuGet/程序集、第二个生态样板。理由：R1 未发现 NoOp 外真实第三方需求信号；NoOp 已是真实注册路径与契约 probe；BUE-PLATFORM-001 已提供运行时防双装兜底；模板/验证器扩工具面却无可量化需求。SDK 首要价值=减少开发者猜测，不是提前建设工具生态。模板/验证器进地图雾区，出现真实生态接入规模或重复错误证据再立票。

### 2. 具名子问题：Contracts 拆分四条件 = 继续暂缓，正式登记为 SDK 门禁条款

逐条：①第三方需脱离完整 BUE DLL 编译=**未触发**；②多仓库需稳定纯契约包=**未触发**；③runtime 与 SDK 发布节奏须独立=**未触发**；④需公开桥接 adapter 而不暴露主程序集=**未触发**。当前维持：第三方直接引用 BetterUnturnedExperience.dll+CopyLocal=false+禁捆绑。四条件写入 SDK 正文或附录，逐条含：条件定义/当前事实判定/触发信号/触发后重评义务/不得提前拆分的当前结论。潜在先触发项=①编译脱耦需求、③发布节奏分化——但「可能先触发」≠「已触发」。**本票不实施拆分、不创建拆分预案工程**。

### 3. SDK 文档结构 = 正文八节冻结，新增附录 A/B/C

附录 A 平台服务参考（按前序票分节：A.1 Admission/Bootstrap、A.2 Feature Events、A.3 Lifecycle、A.4 Network、A.5 HostTick、A.6 Settings、A.7 Diagnostics/Logger；每节只记录对应票已裁决的 interface/顺序约束/线程语义/错误模式/版本规则/同权检验/不承诺项）。附录 B 诊断与身份码表（BUE-REG-001..010、BUE-PLATFORM-001/002、BUE-* 保留前缀、生态诊断码命名规则、FeatureId 官方保留段、生态 FeatureId 合法/非法示例）。附录 C 契约版本与迁移（2.0→2.1 加性变化、T2..T8 Minor 条目、旧契约安全降级原则、破坏性 Major 纪律、四条件、RELEASES 注记要求）。附录不得把 FeatureRegistrationRuntime/BueRuntimeLog 等内部 implementation 误列为公共契约。

### 4. 生态 DLL 验收形态 = 不加第二样板，新增上架前自检清单

NoOpFixture 经前序票扩展为统一 contract probe：独立 BepInEx DLL→HardDependency→Register→Bootstrap→Events→TryTrack/Lifecycle→Network→HostTick→Settings→Logger→停止与隔离。NoOp 不代表真实业务功能、不替代三环境实机验收。**新增「生态 DLL 上架前自检清单」**（人工核对）：BepInEx 插件入口存在/HardDependency 指向 BUE GUID/引用正确版本/CopyLocal=false/未捆绑 BUE/FeatureId 不用官方保留段/DiagnosticId 不用 BUE-*/对未知 NetworkSendResult 安全降级/不调用内部宿主控制面/停止时释放事件网络 Tick 资源/不把 LogOutput、CaseId、RELEASES 当运行时契约。U3DS 专用样板不在决策层预建（属 DEV-V3 实施票与真实环境验收）。

### 5. 移交条目总账（T10//to-spec/实施票三用清单）

T1→SDK：FeatureId 命名指引、契约面定义、官方/生态两层模型、同权和发布纪律。T2→SDK：Admission 三类型、Phase/Reason/Result 语义、BUE-REG-001..010、Bootstrap 成员承诺表、2.0→2.1 迁移说明。T3→SDK：IFeatureEventSubscriber、IOwnedFeatureEventPublisher、EventId 与类型归属、本地事件总线限制、事件异常和注销语义。T4→SDK：FeatureState、FeatureStatusView、StateRevision、IFeatureLifetime.TryTrack、Dependencies 只读查询、CoreSafeMode 触发面、两代际分离。T5→SDK：入站回调线程、主线程 dispatcher、Throttled、Sessions established-only、发送失败和链路健康语义、业务退避由功能自理。T6→SDK：HostTick 八条语义、主线程构造性保证、序号/DeltaTime/Phase、功能侧自节流、无独立 Hz 承诺。T7→SDK：Settings view、ClientPreference/ServerAuthority、revision、schemaVersion、功能自理迁移、会话覆盖、面板不是第二事实源。T8→SDK：IFeatureLogger、结构化字段、BUE-* 前缀纪律、摘要聚合、BUE 写入 BepInEx LogOutput、UMM 人工导出、采集不等于验收。每条必须最终映射到 SDK 具体章节或附录位置，防止前序票契约条目遗漏。

### 6. 契约影响 = 本票自身零契约版本变化

本票主要做文档组织；四条件维持暂缓不是契约变更；附录登记不改变已有 interface；SDK 文档不独立创造运行时成员；Contracts.dll 不拆分。前序票 Minor 变化由各票承担（T2..T8 加性→共用 2.1 或按批次顺延 2.2）。**SDK 文档随 BUE 主 DLL 契约版本走**：不独立发版、不独立交付程序集、与主 DLL 版本/RELEASES 记录绑定；只有未来满足四条件并重新裁决后才允许讨论独立 Contracts 发布节奏。

### 7. 同权与发布物

①文档示例必须锚定 NoOpFixture：不写脱离实际代码的纸上伪示例作唯一依据——生态路径示例回指 NoOpFeaturePlugin.cs（独立 DLL→HardDependency→Register→平台服务消费），文档与活样板双向锚定（样板证明文档可执行，文档解释样板为何这样写）。②SDK 文档随实施发布节奏同步：v8 交付包内 SDK 文档=2.0 版本基线；DEV-V3 实施票产生 2.1 契约变化时：源码契约更新→SDK 文档同步→对应候选 DLL→实机/审查/CaseId→publish 交付包同步换新→RELEASES 记录对应版本。属实施期同步事项，不在 T9 决策阶段产候选或改 RELEASES。

### 本票不做

项目模板、编译期验证 CLI、第二生态样板、Contracts.dll 拆分、独立 SDK 程序集、SDK 独立发版、自动 RELEASES、U3DS 专用生态样板、新的运行时 interface。

## Comments
