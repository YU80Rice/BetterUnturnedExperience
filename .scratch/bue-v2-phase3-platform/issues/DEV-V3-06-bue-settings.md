# DEV-V3-06：BueSettings 接线与面板动态路由（官方生态共用+双 scope）

Type: task
Status: ready-for-agent
Parent: spec.md（V2 第三阶段规格·生态开发者平台能力定界与接线）
Blocked by: DEV-V3-03（BueLifecycle）
Spec: `../spec.md`（「设置（V3-T7 → DEV-V3-06）」节）

## What to build

生态作者的设置与官方功能同纪律：经注入的 `bootstrap.Settings` 读快照、提交变更并观察 revision 推进与校验拒绝；自己的设置在管理面板与官方设置同样可见可编辑（面板按注册目录动态路由）；ClientPreference/ServerAuthority 双 scope 下 U3DS 与 P2P 主机权威端同语义，会话覆盖断线清除不污染持久化 revision。

## Scope

- `bootstrap.Settings` 接线（可用性矩阵行，红线钉「接线前 null+接线后可用」两侧）：view 限当前功能作用域（GetSnapshot/TryGet/Submit）。
- 官方与生态共用 SettingsRuntime 规则：校验/revision 单调/损坏安全默认/原子提交/作用域隔离；类本体不列契约；生态五不得（不另造配置格式、不绕过作用域、不直写持久化、不假设他人作用域可读、不做跨机同步）。
- 面板按注册目录动态路由（官方硬编码清单退役）；面板=编辑 adapter 非第二事实源；未提供设置的功能不伪造设置页。
- ClientPreference/ServerAuthority 双 scope：权威端两环境（U3DS 与 P2P 主机）同语义；客户端会话覆盖断线清除、永不污染持久化 revision；不做跨机同步协议。
- schemaVersion 通道保留，迁移由功能自理；`ExpectedRevision` 防旧 UI 覆盖新值。
- 不做：SettingsRuntime 与 BueNetwork 职责混合；跨机同步；平台统一迁移框架。

## 验收条件

- [ ] 红测先行：InMemorySettingsPersistence 组（快照/提交/校验失败/ExpectedRevision 过期/损坏安全默认/作用域隔离/断线覆盖清除），各先红后绿（先例=Settings 测试工程既有组）
- [ ] 矩阵接线两侧红测：Settings 接线前 null+接线后可用；面板动态路由（官方+生态条目并列、未提供设置者无页）
- [ ] 官方先行消费锚：官方功能走真注入 Settings view（非内部控制面）断言
- [ ] 双轴独立审查（每轮全新实例）CLEAN；全套测试 0 警告 0 错误
- [ ] **候选纪律**：本票不产正式候选 DLL、不更新 RELEASES、不授 CaseId（中间构建=开发态内部基线）

## Comments（实施票面登记——冻结面变更清单/本票定值/语义裁决/具名递延）

状态推进：ready-for-agent → in-progress（独立会话 /implement，2026-09-10）。

### 1. 冻结面变更清单（Minor 2.1 加性，SDK 附录 A/B 归 DEV-V3-08 逐码落档）

- `IFeatureSettingsRegistration`（新公开契约接口，设置 facet）：注册对象可**额外实现**的可选面（宿主经 `as` 类型发现；2.0/无设置注册不实现=无平台管理设置，旧实现零破坏——与 IFeatureRegistration 加成员会运行时打断外部实现者相区分）。恰两成员：`IReadOnlyList<SettingDescriptor> SettingDescriptors`（功能拥有 Schema，宿主据此构造唯一 per-feature SettingsRuntime）+ `Action OnSettingsApplied`（面板编辑生效后的功能私有刷新钩子，可 null；功能仍拥有运行时读取，面板不持业务状态）。
- `IFeatureRegistration` 形状**不变**（恰 4 属性，红锚钉死）；`IFeatureBootstrap` **不变**（恰 11 成员，05 锚继续成立）；`IScopedFeatureSettings` **不变**（恰 3 方法 GetSnapshot/TryGet/Submit，新形状锚钉死「查询面不得扩」——视图注入面永不含 ApplyServerPolicy/ClearSessionOverlay/ActivateConnectionGeneration 类运行时方法）。
- `FeatureRegistrationEntry`（Core 公共目录投影，非 SDK 契约承诺面）加性两属性：`SettingDescriptors`（null=无 facet）+ `OnSettingsApplied`；登记记录/快照同步携带（owner-scoped 事实载体纪律）。
- 登记码表扩展：`BUE-REG-011`=设置 facet 无效（reason=InvalidDefinitionArtifact 复用冻结枚举值，不扩枚举）。
- 诊断码族本票定（宿主观察行不入附录 B 拒绝码表，BUE-MT-GEN/BUE-CLOCK-001 先例）：
  - `BUE-SET-001` 视图写入被拒：代际失效/停止边界后（generation-invalid）；
  - `BUE-SET-002` 视图写入被拒：ServerAuthority scope 非权威端（not-authority-side）;
  - `BUE-SET-003` 同功能 schema 冲突：注册表 KeepExisting 显式拒（防第二事实源）；
  - `BUE-SET-004` 面板回退编辑器拒编辑未提供设置的功能（不伪造设置页）；
  - `BUE-SET-005` 无效 schema 抵达注册表（登记侧同门=BUE-REG-011；直接构造注册表=宿主内部带子，均显式行不落空）；
  - `BUE-SET-CREATED` 组合行（每功能 runtime 恰一条）；
  - `BUE-SET-GEN` 代际边界观察行（generation-opened/owner-invalidated/composition invalidate 失败=BUE-MT-GEN/BUE-CLOCK-001 先例，宿主观察行不入附录 B 拒绝码表）。
  - R1-Spec 修复注记：005/GEN 两码为 F1 轮补登记（实现先于登记=02 R1 形态 gap，此段即闭合证据）。

### 2. 本票定值（先例=03 容量 64/04 预算 256）

- facet 描述符上限 **64/功能**（超限=BUE-REG-011 拒注册，可观察可测试）。
- 持久化 root 沿用 `Unturned\BetterUnturnedExperience\`（`<FeatureId>.<Scope>.bue-settings` 文件布局不变=玩家既有设置文件无缝续用；契约侧不硬编码路径纪律不变）。

### 3. 语义裁决

- **接线形状**：`BueFeatureStartRuntime.ComposeBootstrap` 经 `BueSettingsRuntime`（Plugin 组合根，BueMainThreadRuntime 先例）按 root 键控的 `FeatureSettingsRegistry`（Core，public 非 SDK：宿主构造 per-feature SettingsRuntime 唯一实例+代际账+权威侧门+视图工厂）组合注入；OpenGeneration/InvalidateOwner 与启动/停止/隔离边界同步（MainThread dispatcher 同构）。视图 `FeatureScopedSettingsView` 绑 (feature, LifecycleGeneration)：**读**（GetSnapshot/TryGet）永可用（功能自身持久真相的只读观察，无突变=无跨代污染面）；**写**（Submit）代际失效/停止边界后显式拒+BUE-SET-001；ServerAuthority scope 写在非权威端显式拒（UnauthorizedSender）+BUE-SET-002；权威端 U3DS 与 P2P 主机=同一 provider 同一代码路径=同语义（红锚双侧钉）。
- **矩阵两侧红线**：有 facet 的探针经真实 StartCatalog=非 null 且可用（接线后可用侧）；无 facet 功能=null（「未提供=不伪造」侧+阶段基线纪律）；Logger 行仍 null（07 前红线保留）。
- **面板=编辑 adapter 非第二事实源**：路由/快照全部来自注册目录 facet+宿主唯一 runtime；面板 GetSnapshot 与功能 view GetSnapshot 恒等（同 revision 同 entries）；面板编辑→runtime→OnSettingsApplied→功能经注入 view 读到新值（全链单真相）。官方五路（network/v1compat/LIT/LIR/LHT）硬编码路由退役=BII 组合根编辑器保留为显式路由（插件自身 UI 功能非「清单」），其余一律目录驱动，回退=Unavailable 拒编辑。
- **官方先行消费**：真实 LIT 改经注入 view（模块不再构造/持有 SettingsRuntime；构造器 persistence 参数退役=红链主体）；LIR/LHT/NetworkModuleAdapter 同步迁移（同 runtime 唯一性使宿主侧必须单实例，双 runtime 同文件=第二事实源违规，故五路全迁而非仅一）。
- **双 scope/会话覆盖/断线清除**：SettingsRuntime 既有 overlay 机制经视图暴露读侧（SessionProjection 源+revision 不污染）；断线清除=宿主内部 runtime API 驱动（生产服务器→客户端 policy 投递无传输通道=跨机同步另立票裁决照登）；红锚=视图层覆盖生效/清除恢复偏好/持久 revision 全程不动。
- **面板启停（03 具名移交「面板按钮接线随 06 动态路由落地」）**：ManagementPanelModel 加 `TryToggleFeature` command adapter→`BueFeatureStartRuntime.SetFeatureEnabled`（面板=command adapter，状态机归宿主），条目 State 从硬编码 Running 改为生命周期机器实时投影（启停后面板如实可见）；原生面板**按钮 UI**（Unity 控件）交付=具名递延（宿主侧只锚模型 seam+状态投影，实机按钮随 09 三环境验收面）。

### 4. 具名递延（不静默）

- 跨机设置同步协议/服务器→客户端 policy 投递与断线清除的**生产驱动**：Network 域另立票（T7 §3 裁决照登）。
- SDK 附录 A「Settings 节」逐码/矩阵行/双 scope 文档落档：DEV-V3-08。
- NoOp probe 全链统一 probe：DEV-V3-08（本票只扩 Settings 支线对照字段）。
- 原生面板启停按钮 UI：见上，随 09。
- SettingsRuntime 类本体/ISettingsPersistence/面板 editor 类本体继续不列契约（类本体自由不变）。

### 结单（2026-09-10）：resolved——双轴 R2 双 CLEAN，全套 7/7 PASS+Rebuild 0 警 0 错

红绿链：行为红 4+编译红 CS0246 三工程→实现→ALL GREEN；红链期间修复红测自身缺陷三处具名（descriptor 归属×组、settingsRoots 作用域、Action 二义）。双轴链（每轮全新实例）：R1 Standards CLEAN(6deferrable：死类/未用 using/BOM/锁内构造/同形保留/码表)/Spec FINDINGS(2gap：并列红锚未落+SET-005/GEN 未登记)→F1(新增官方与生态并列子组+码表补登+删死类 RoutingBueSettingsEditor+锁内构造具名不变量理由+未用 using 清)→R2 双 CLEAN(Standards 2deferrable 保留：组合根装配参数残面/文件命名)。候选纪律守住：无候选 DLL/无 RELEASES 变更/无 CaseId。结单报告+final 证据=audit/2026-09-10/DEV-V3-06/。

### 5. 红测计划（批序：锚点壳→断言组→注册块）

- Settings.Tests：`FeatureSettingsRegistry`+视图组（快照/提交/校验失败/ExpectedRevision 过期/损坏安全默认/作用域隔离/断线覆盖清除/代际失效写拒/权威侧门/schema 冲突）。
- Contracts.Tests：facet 恰两成员/IFeatureRegistration 恰 4/IScopedFeatureSettings 恰 3 方法（隐式扩面必红）。
- Plugin.Tests：`AssertBueV3SettingsWiringAndPanelRouting`（矩阵两侧/目录路由官方+生态并列/未提供无页/官方先行消费 LIT 全链/停止边界写拒+重启续用/权威双侧同语义/NoOp 支线/BUE-REG-011 逐例）+`--bue-v3-settings-red`；01 矩阵组接线侧断言翻转（Settings→非 null）。
- 既有 V2 触点（module.Settings/adapter 网络开关测试约 48 处）随生产迁移行为等效改写=编译红主体。
