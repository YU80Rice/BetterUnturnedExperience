# V3-T1 跨阶梯共享裁决

- **Ticket**: V3-T1
- **Type**: grilling（决策票，HITL；必调 grilling + domain-modeling）
- **Status**: resolved（2026-09-09 六项裁决定音）
- **Blocked By**: —
- **Map**: [map.md](../map.md)

## Question

只处理跨所有阶梯的共同规则（目的地框架已由 2026-09-09 开图轮 Q1 裁决定音，本票不重开）。逐项裁决并输出为可被 T2..T10 直接引用的**共享裁决条目**：

1. **官方-生态同权**：同权模型在本阶段的表述与检验口径（官方功能与生态功能在公开契约上同权，官方不得保留框架私有特权）；
2. **生态 DLL 发现**：由 BepInEx 原生发现的承诺措辞与边界（BUE 不重复发现、不自建扫描器/loader）；
3. **公开契约版本纪律**：Major/Minor 演化规则与破坏性公告流程在本阶段的适用面——重申或修订；
4. **身份纪律**：FeatureId / AssemblyName / BUE GUID 的冻结面重申（开发者契约唯一事实源 `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md` 现行措辞是否需要修订）；
5. **RELEASES 交付与证据绑定**：CaseId / 轻量链 / 候选节奏在 DEV-V3-* 上的延续规则；
6. **两层交付模型**：单 DLL 官方交付 + 独立生态 DLL 并存（对账注记 3 冻结基线）的再确认。

每项输出：裁决条目 + 受影响的既有冻结词汇/文档条目清单（如有修订，同步 CONTEXT.md / SDK 文档）。

## Answer

2026-09-09 用户+PM 六项裁决全部定音（本节为冻结条目，T2..T10 直接引用）。本票产出=跨阶梯共享规则 module：interface 小、约束明确，不含任何阶梯实现方案。

### 1. 契约面同权（+官方先行消费门禁）

- **同权=契约面同权**：凡 BUE 列入公开开发者契约的能力，官方与生态必须经同一公开 interface、同一套注册/生命周期规则、同一套错误与隔离语义使用；不得为官方保留绕过公开契约的私有捷径。同权含：同一注册桥 / IFeatureBootstrap / BueNetworkApi / 功能事件与宿主时钟订阅路径 / 设置、诊断、模块状态语义 / 失败隔离与注销规则 / 版本与兼容承诺。
- **同权不承诺**：相同业务能力；必须使用所有平台能力；客户端 UI/服务器权限/原生 adapter 必然对两者可用；私有实现立即全部重构成公开接口。
- 现状（入口同权、服务消费未全同权，见 V3-R1）**不追溯否定**，私有路径上收与否由 T2..T9 分别裁决。
- **检验门禁=官方先行消费（dogfooding）**：新增/扩展的公开契约面至少一个官方功能真实消费并通过测试；NoOpFixture 只证明生态注册路径可运行，不单独证明平台服务被官方消费。
- 各票「同权影响」五问：改了哪个契约面 / 官方是否真实消费 / 生态能否同面接入 / 不可用是否同样显式 unavailable-rejected / 有无官方专用绕行。

### 2. 生态 DLL 发现承诺（纯重申）

发现、排序、实例化由 BepInEx 原生承担；BUE 不建 DLL scanner、外部 module loader、重复排序系统。现行 SDK 文档措辞不变（普通 BepInEx 插件发布、`[BepInDependency]` 声明前置、不承诺替代早期加载/Preloader/内部发现细节、BUE 只提供注册桥+平台服务）。不再立票、不入 T2..T9 实现范围。

### 3. 公开契约版本纪律

- **契约面 = SDK 文档明确列举、承诺并登记的成员；`public` ≠ public contract**——未登记 public 面（如 `FeatureRegistrationRuntime` 类宿主内部类型）不属稳定承诺、生态不得依赖、可在非破坏性公告之外调整、不作为兼容性依据。
- 版本规则：破坏性=Major+SDK 文档公告+RELEASES 行注记；新增向后兼容=Minor；纯实现修复不改契约语义=Patch 或不动契约版本；冻结面新增成员须同步 SDK 文档+测试+契约清单；冻结成员语义变化=按破坏性处理（不得借「仍 public」规避）。
- 各票「契约影响」六问：是否触及 SDK 冻结面 / 触及哪些成员 / Major-Minor / 文档与 RELEASES 注记 / 是否纯内部变化 / 是否产生新公开 seam。

### 4. 身份纪律 + 生态 FeatureId 命名指引

- 现行冻结重申：BUE GUID 冻结、AssemblyName 冻结、文件名/部署路径非契约身份、FeatureId≠GUID、显示名/slug/程序集名不可替代 FeatureId、同 GUID/同 AssemblyName 处理遵守 BepInEx 与 BUE 现有诊断边界。
- **新增**：`io.github.yu80rice.bue.*` 为**官方保留段**；生态 FeatureId 必须用作者自己的反向域名（例 `com.authorname.inventoryhelper` / `net.example.servertools.hud` / `org.teamname.bue.extension`）；不得冒用官方 FeatureId、不得用显示名/文件名/程序集名当唯一身份；注册桥发现保留段冲突必须拒绝并返回明确诊断；SDK 文档须给合法+非法示例。这不是新建 FeatureId 注册系统，是既有注册桥上的命名治理。
- **SDK 文档修订条目（随 /to-spec 落地）**：FeatureId 命名指引（含合法/非法示例）+ 契约面定义（public≠契约）登记。

### 5. RELEASES 交付与证据绑定

- **决策票纪律（本图即生效）**：V3-T* plan-only——不产候选 DLL、不加 RELEASES 行、不因地图/规格决策授予候选身份、研究结果不当发布证据、静态审查 PASS≠运行验收 PASS。
- **DEV-V3-* 实施票沿用现行节奏**：红测→绿测→双轴 CLEAN→必要实机验收→SHA-256/CaseId 绑定→RELEASES 行；轻量链/完整链按票型定；每个候选绑定源码基线+DLL SHA-256+构建记录+测试记录+CaseId+本票审查验收证据。
- **证据链断裂治理**（auto-evidence 空目录、哈希归档手工化、诊断包自动采集）全部归 **V3-T8**；T1 只记录「现行证据绑定纪律继续有效，自动化治理由 BueDiagnostics 单独裁决」。

### 6. 两层交付模型（纯重申）

官方层=BII+LIT+LIR+LHT 源码模块→构建期聚合→唯一 `BetterUnturnedExperience.dll`；生态层=第三方独立 DLL→普通 BepInEx 插件→原生发现→`[BepInDependency]`（Hard）→引用主 DLL+CopyLocal=false+禁捆绑→`BueRuntimeHost.Register`→消费公开平台服务。共同点=同一公开契约/注册桥/生命周期·事件·网络·设置·Tick·诊断·隔离规则/契约面同权；不同点=官方随发行版交付并担维护验证责任、生态独立发布不随官方发行版交付、BepInEx 负责发现两者而 BUE 不重复发现。

### 本票明确不做

不建 DLL scanner / 外部 loader；不重做 BepInEx 发现排序；不立即拆 Contracts.dll；不在 T1 实现诊断自动采集；不在 T1 重构所有官方私有路径。

## Comments
