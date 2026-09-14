# 《更好的未转变者体验》V1 需求规格（中文版）

> **SUPERSEDED / 历史资料，不是现行契约**（2026-09-14 落标）：本文件是 V1 英文需求规格的中文镜像，随英文版一并退役，不得作为现行契约或实施依据。
> 现行生态契约唯一事实源 = `docs/sdk/BetterUnturnedExperience-SDK-Assembly-Identity.md`；人类开发者入口 = `docs/developer/README.md`。本文件保留仅作历史决策资料，不删除。

> 基础设施镜像文件；作者：GPT。  
> 状态：ready-for-agent  
> 阶段：需求规格  
> 语言：面向人工开发者的简体中文副本  
> 英文 Agent 执行权威版：[spec.md](spec.md)  
> 同步规则：本文件与英文版使用相同规格版本、标题结构、契约签名和验收语义；任何需求变更必须在同一变更中同步两份文件。若翻译产生歧义，以英文精确契约签名和双方复核后的原始决策为准，但不得以此为由遗漏中文更新。

## 问题陈述

《更好的未转变者体验》的目标，是为《未转变者》原版功能优化提供一个规范化、开放、可自定义、兼容性高、支持多创作者源码协作的插件框架。首个接入并实现的功能“更好的物品交互”，用于改善原版物品栏和容器栏中的拖动、候选摆放和放置反馈体验。两者都必须兼容单人、SteamP2PFriends Host/Client 和 U3DS。

产品面向两类使用者：玩家通过统一设置入口使用各项原版体验增强；开发者通过稳定共享契约、功能定义管线和模块注册机制贡献、组合及维护功能。框架不能要求开发者重复制作设置菜单、网络协议或生命周期基础设施，也不能让单一功能故障破坏其他功能或原版游戏体验。

主要风险是前端、后端和共享契约在接触具体 Unturned 类型与成员时发生职责漂移：前端自行实现库存规则、后端泄露 UI 类型、双方采用不同函数名/坐标/状态机/错误顺序，或在缺少固定源码证据时猜测 Harmony hook。

因此本规格分成三层：先冻结前后端共同依赖的精确逻辑和接口；再由 Gemini 调研前端 U3-SDK adapter；由 GPT 调研后端 U3-SDK 和权威调用链。调研只能补充 adapter 实现事实，不能擅自改写共享契约。共同接口变更必须重新进行 GPT/Gemini 双端复核。

## 解决方案

1. **共享契约层**：GPT 维护；冻结生命周期、设置、事件、候选计算、状态投影、错误码、版本和 intended item center 坐标语义。Gemini 与 GPT 只依赖此层，不依赖对方内部实现。
2. **Gemini 前端 U3-SDK adapter 层**：把 Glazier/Sleek UI、鼠标指针、抓取偏移、旋转输入、菜单生命周期和原生库存投影转换成共享契约输入与只读表现。
3. **GPT 后端 U3-SDK adapter 层**：把 `PlayerInventory`、`Items`、原生 RPC、服务端复验、线程模型、存储上下文和网络能力转换成共享契约状态与权威操作。

V1 最终交付一个聚合 DLL。功能作者通过源码贡献和规范功能定义接入；V1 不动态加载外部功能 DLL。库存最终提交继续使用 Unturned 原生权威路径，不创建平行库存协议，不进行乐观库存修改。

### 产品验收结果

- **“更好的物品交互”**：玩家能在受支持的物品栏和容器栏中正常拖动、预览、自动旋转，并通过原生权威路径完成放置；无候选位置时取消并保留原位置；功能禁用或隔离后原版交互仍可用。
- **“更好的未转变者体验”框架**：“更好的物品交互”通过框架注册并实际生效；至少再用一个由项目开发者提供的接入验证模块，证明稳定公共契约、统一设置入口、生命周期/隔离和单 DLL 链接机制能承载其他插件功能，并与首发功能同时正常运行。该模块可以是不进入发布 DLL 的测试夹具，不算第二个首发产品功能。
- “正常运行”必须分别取得单人、SteamP2PFriends Host/Client 和 U3DS 适用角色的运行证据，并绑定同一 Candidate DLL SHA-256。构建成功或静态审查不能替代玩家可观察的运行验收。

## 用户故事

### 玩家与运行环境

1. 玩家不必精确对准单格即可拖放物品。
2. 玩家能在释放前看见准确占据范围，并区分合法绿框与阻挡红框。
3. 无候选位置时物品保留原位，不发生可避免的丢失。
4. 只有手边空间需要时才自动旋转，开阔区域不反复翻转。
5. 手动旋转保持抓取点，拖动图标不应异常跳动。
6. 靠近边缘时候选自然贴边并保持可预测。
7. 提交后增强预览立即消失；迟到的服务器投影静默成为最新事实。
8. 增强功能不可用时原版物品栏仍可正常使用。
9. 整个产品只有一个统一设置入口；本地偏好被服务器政策覆盖时仍保留，并明确显示政策锁定。
10. 模块故障仅显示本地化、可操作状态，不向普通玩家暴露异常堆栈。
11. 单一功能故障不影响其他功能；核心 SafeMode 不应主动终止原版游戏。
12. 单人使用与多人使用相同规则；SteamP2PFriends Host 本地执行权威规则，Client 只把服务端确认和原生库存投影当作事实。
13. U3DS 无需解析 Glazier 或客户端类型；错误日志必须包含 FeatureId、版本和 DiagnosticId。

### 功能作者、维护者与测试者

14. FeatureId 独立于目录名和显示名且永久稳定。
15. 静态设置描述进入构建期定义产物，功能作者无需创建自有菜单。
16. 设置、日志、事件和能力按功能作用域隔离；长期注册项受生命周期跟踪并可可靠清理。
17. ClientUi 注册记录由构建期生成，不依赖反射扫描或类名猜测。
18. 必需依赖按确定顺序启动；可选依赖通过能力视图查询。
19. 契约兼容由结构化版本决定；非法声明在定义编译期被拒绝。
20. 前端只调用一次候选计算即可取得全部渲染事实，不重写放置规则。
21. pointer 到 intended center 的转换属于 ClientUi adapter；静态 Settings facet 与动态快照不能成为两个事实源。
22. timer/callback 必须受当前 generation 保护；UI hook、缩放、裁剪、层级和 U3DS 类型隔离必须有固定证据。
23. 后端必须记录原生库存 RPC 全链、所有权/页面/容量/占用/装备位验证、线程边界、容器会话和存储隔离。
24. LMN channel、handler、payload 和线程行为必须根据固定源码验证；不得把它宣传成库存权威替代物。
25. 共享契约通过公共接口测试；前后端研究事实标注源码、IL、原型或运行证据类别。
26. CandidateBuild、环境证据和发布批准互相独立；SP、P2P Host/Client、U3DS 必须绑定同一 DLL 哈希。
27. 共享接口变更必须由 GPT 更新并经 Gemini 确认可消费；研究报告必须写出推荐 adapter、被拒方案和未决测试义务。

## 实施决策

### 第一层——前后端共享契约

#### 所有权和依赖方向

- GPT 是共享契约维护者；任何由前端消费的字段、函数、事件或兼容性变更都必须由 Gemini 复核。
- Contracts 只包含兼容 .NET Framework 的领域类型，不引用 BepInEx、Harmony、Glazier、Sleek、Steamworks、LMN 或 Unturned 具体类型。
- 前端和后端 adapter 依赖 Contracts；Contracts 不依赖 adapter。
- 禁止运行时模块自行声明身份、依赖、设置 schema、事件所有权、能力或入口绑定；这些事实来自链接后的单一定义产物。

#### 冻结的函数名

| 接口 | 精确成员 | 必须行为 |
| --- | --- | --- |
| `IFeatureModule` | `FeatureStartResult Start(IFeatureBootstrap bootstrap)` | 仅在可信准入、设置 bootstrap 完成并进入 Starting 后调用。 |
| `IFeatureModule` | `void Stop(FeatureStopReason reason)` | 每个 lifecycle generation 最多调用一次；语义清理由 lifetime lease 清理兜底。 |
| `IFeatureLifetime` | `bool TryTrack(IDisposable registration)` | 跟踪长期注册；拒绝 null；Closing 后的迟到注册立即释放。 |
| `IScopedFeatureSettings` | `FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope)` | 返回当前功能作用域的完整不可变快照。 |
| `IScopedFeatureSettings` | `bool TryGet(string settingId, out SettingValue value, out uint revision)` | 读取一个确认值，不接受任意 FeatureId。 |
| `IScopedFeatureSettings` | `SettingChangeResult Submit(ScopedSettingChangeRequest request)` | 执行一次原子的功能作用域设置事务。 |
| `IFeatureEventSubscriber` | `IDisposable Subscribe<TEvent>(Action<TEvent> handler)` | 只订阅链接后 consumption facet 允许的事件。 |
| `IOwnedFeatureEventPublisher` | `bool TryPublish<TEvent>(string declaredEventId, TEvent value)` | 只发布当前功能拥有、已声明且类型完全匹配的事件。 |
| `IFeatureLogger` | `void Info(string eventName, string diagnosticId)` | 输出功能作用域信息诊断。 |
| `IFeatureLogger` | `void Warning(string eventName, FrameworkErrorCode error, string diagnosticId)` | 输出稳定警告码，不向玩家暴露原始 payload。 |
| `IFeatureLogger` | `void Error(string eventName, FrameworkErrorCode error, string diagnosticId, Exception exception)` | 异常仅内部记录，普通 UI 只收到安全投影。 |
| `IDependencyCapabilityView` | `bool Has(string declaredDependencyId, string capabilityId, ushort minimumVersion)` | 只查询链接后的本地依赖 ID。 |
| `IDependencyCapabilityView` | `bool TryGet(string declaredDependencyId, out NegotiatedFeatureView feature)` | 返回当前可见的协商依赖投影。 |
| `IPlacementCandidateEvaluator` | `ItemPlacementPreview Evaluate(PlacementCandidateInput input)` | 纯、同步、无状态，无 Unity 副作用；生产目标为每次调用零分配。 |

#### 冻结的 bootstrap 属性

`IFeatureBootstrap` 只暴露以下属性：

| 属性 | 类型 |
| --- | --- |
| `Identity` | `FeatureScopeIdentity` |
| `LifecycleGeneration` | `ulong` |
| `Settings` | `IScopedFeatureSettings` |
| `Events` | `IFeatureEventSubscriber` |
| `OwnedEvents` | `IOwnedFeatureEventPublisher` |
| `Logger` | `IFeatureLogger` |
| `Dependencies` | `IDependencyCapabilityView` |
| `Lifetime` | `IFeatureLifetime` |

禁止暴露 UI root、原生库存对象、LMN connection 对象和任意模块查找能力。

#### 冻结的 Command/Event 契约

| Kind | 契约类型 | 方向/范围 | 关联与 payload 事实 |
| --- | --- | --- | --- |
| `0x0101` | `UpdateModuleConfigCommand` | Ready 后 client→server；`ClientPreference` 仅进程内 | `RequestId`、`Feature`、`RevisionScope`、`ExpectedRevision`、完整 mutation；网络只允许 `ServerAuthority`。 |
| `0x0102` | `ModuleConfigChangedEvent` | Ready 后 server→client；确认的本地变更也可进程内 | 回显 `RequestId`；包含 `Feature`、`RevisionScope`、新 `Revision` 和完整 `FeatureSettingsSnapshot`。 |
| `0x0103` | `ModuleConfigRejectedEvent` | Ready 后 server→client | 回显 `RequestId`；包含 `Feature`、`RevisionScope`、稳定错误、`CurrentRevision` 和完整当前快照。 |
| `0x0104` | `RequestModuleConfigSnapshotCommand` | Ready 后 client→server | 使用新的非零 `RequestId`，包含 `Feature`、`RevisionScope`、`KnownRevision`；只读且不得增加 revision。 |
| `0x0201` | `FeatureStatusChangedEvent` | 对该客户端可见的 server/negotiated 状态由 server→client；另允许进程内投影 | 携带完整 `FeatureStatusView`；前端按 revision 和安全诊断身份去重。 |
| 仅进程内 | `CoreRuntimeStatusChangedEvent` | core→本地前端；V1 不注册为 LMN | 携带 revision 单调递增的完整 `CoreRuntimeStatusView`。 |
| `0x0004` | `SessionReadyEvent` | capabilities channel 上 server→client | 关联 `ConnectionGeneration` 和 `SnapshotId`；此前禁止设置/状态网络消息。 |

所有网络消息使用共享 Contract envelope 和冻结 channel 映射。`RequestId` 只在 connection generation 内提供事务关联/幂等，不代表身份或授权。revision 与 connection generation 是不同排序域。

V1 不把 `CommitItemPlacementCommand`、`RequestRotateItemCommand`、`ItemPlacementCommittedEvent`、`ItemPlacementRejectedEvent`、`InventoryStateUpdatedEvent` 注册为插件网络消息。

#### 冻结的交互、状态与坐标

- FeatureState 共九种：Discovered、Incompatible、Disabled、Starting、Running、Isolating、Isolated、Stopping、Stopped。
- ModuleRuntime/Lifecycle 是 FeatureState、StateRevision 和状态事件的唯一写入者。
- Runtime Admission 只返回静态准入，不写生命周期状态、不读用户设置；SettingsRuntime 完成迁移/验证后，Lifecycle 才选择 Starting 或 Disabled。
- 拖动顺序：Idle → Dragging → Hovering → Dropping → AwaitingProjection → Idle。
- AwaitingProjection 的 2.0 秒超时只结束增强 UI 等待，不推断服务端拒绝。
- 设置提交三秒后用同一 RequestId 重试一次；八秒后用新 RequestId 通过 `0x0104` 请求完整快照。
- 容器和 footprint 原点在左上，X 向右、Y 向下。
- `grabOffsetInFootprint` 是前端连续坐标，闭区间 `[0,W] × [0,H]`。
- `CursorGridX/Y` 是历史 Draft 名称，承载 intended item center，而非原始 pointer。
- intended center = pointer grid + 当前 footprint 中心 − grab offset；容器有效域为半开区间 `[0,containerWidth) × [0,containerHeight)`，超出返回 Hidden/OutsideGrid。
- 原生正向 `rot+1`：W×H 到 H×W 的抓取偏移转换为 `(H-gy, gx)`；反向 `rot-1` 为 `(gy, W-gx)`。先转换抓取偏移，再重新计算 intended center。

#### 冻结的候选算法

1. 局部投影的当前朝向能放下时立即返回，不检查旋转。
2. 局部当前朝向失败且局部旋转朝向能放下时，返回旋转候选。
3. 否则按“中心距离平方、Y、X”搜索所有当前朝向候选。
4. 只有当前朝向全局无候选时才搜索旋转朝向。
5. 不自动交换、重排已占用物品。
6. evaluator 只返回预览事实；最终提交走原生库存路径。

具体 native adapter 类型名、Harmony target 和原生事件 hook 不在共享层冻结。研究后才能推荐内部 adapter 名称；新增共享成员必须走契约修订和双端复核。

### 第二层——Gemini 前端 U3-SDK 调研

#### 目标与必查事项

确定客户端 UI 的真实 seam，不能把 UI 类型泄漏到 Contracts/Core，也不能仅凭名称猜测运行行为。必须：

- 追踪物品栏/容器 UI 的构造、打开、关闭、销毁，以及 drag start/update/rotate/release/cancel/page change/container change 的精确成员与签名。
- 确认原生 UI 如何保存源 page/position/rotation/footprint/pointer offset 和类似 generation 的状态。
- 追踪 `PlayerDashboardInventoryUI` 在 rot 0～3 的 pivot/定位，验证 `rot++` 四朝向转换并记录源码行。
- 确认 screen/viewport/UI Scale 到 pointer grid 的 API、cell 像素尺寸、缩放、滚动、裁剪和 Z-order。
- 为 footprint overlay 与浮动图标选择安全 parent/root，不假定一个 root 适用于全部视图。
- 追踪用于刷新 item view 的原生库存投影事件及其顺序，并确认事件能否识别源/目标变化；区分 `onInventoryAdded`/`onInventoryRemoved` 的源码事实与 adapter 假设或待运行验证行为。
- 查明原生放置音效的调用位置，并裁定插件应主动调用还是依赖原生路径；同时查明旋转键、设置快捷键、文本/键位捕获的输入焦点行为。
- 追踪主菜单、暂停菜单和选项菜单的统一设置入口 seam。
- 比较选定客户端 reference 中 Glazier/Sleek 类型差异，列出所有可从 UI 组件到达的客户端专用 type token。
- 提议无需 assembly scanning 的构建期内部注册记录；仅在没有事件/interface seam 时推荐 Harmony patch。
- 记录与其他库存 UI patch 的兼容风险和排序策略，但不得宣称普遍兼容。

#### 必交产物

- Gemini 前缀的前端 U3-SDK 调研报告。
- 固定源码身份、类型、成员、签名、caller、callee 和证据分类的调用链表。
- UI 生命周期图、坐标转换表、与 Contracts 分离的内部 adapter interface 清单。
- 被拒 hook 及原因；未决事实对应的实现测试或运行证据义务。
- 调研产物不得包含生产实现。

### 第三层——GPT 后端 U3-SDK 调研

#### 目标与必查事项

确定实现框架和“更好的物品交互”所需的原生权威、状态、线程、持久化及服务端/客户端 seam，不绕过 Unturned 验证，不创建第二套库存事实。必须：

- 分别追踪 SP、P2P Host loopback、P2P Client 的 `sendDragItem` 到 `ReceiveDragItem`；U3DS 从服务器网络入口开始，不能把客户端方法当作服务器进程起点。
- 记录 RPC reliability、所有权限制、caller 身份来源和速率限制。
- 追踪源 page/item 查找，以及 page、坐标、rotation、capacity、occupancy、asset size、装备位、storage access 的每项验证。
- 记录成功后的 mutation 顺序、mutation 前全部失败出口、remove/add 是否在游戏线程层面原子，以及不安全 callback 点。
- 追踪 `checkSpaceEmpty`、`checkSpaceDrag`、`checkSpaceSwap` 和相关 `Items` 方法，但保持 V1 不自动交换。
- 确定当前打开容器 session 的表示/失效方式、可只读观察的库存投影事件、RPC/inventory mutation 线程。
- 确定 LMN callback 和 feature callback 的安全排队边界；追踪 client/U3DS 的 BepInEx 初始化与关闭。
- 比较 client/U3DS `Assembly-CSharp`/BepInEx reference 身份与类型交集。
- 只为只读上下文捕获/原生提交适配选择安全 Harmony target；拒绝绕过 `ReceiveDragItem` 的 patch。
- 追踪 server/local 设置存储根和 API，验证 LMN V5 named-channel 注册、可靠发送、sender context、payload limit 和 handler thread。
- 区分 Runtime Admission 静态事实与 SettingsRuntime/Lifecycle 动态事实；只投影稳定错误族，不泄露实现细节。
- 所有需要运行验证的假设必须绑定同一 Candidate DLL 哈希。

#### 必交产物

- GPT 前缀的后端 U3-SDK 调研报告。
- 每个环境角色的端到端库存权威调用链、验证/修改矩阵和原生修改前最后安全点。
- 线程/排队图、client/U3DS reference 交集与类型泄漏风险表。
- 与 Contracts 分离的内部后端 adapter interface、被拒 patch 及理由。
- SP、P2P Host/Client、U3DS 运行证据义务。
- 调研产物不得包含生产实现。

### 调研协作规则

- Gemini 与 GPT 可以独立并行调研，但必须使用同一冻结 U3-SDK 源码身份并明确记录。
- 发现共享 DTO/member 不足时，只能提出共享契约变更请求，不得静默实现。
- 共享契约变更只有在 GPT 更新且 Gemini 确认可消费后才成立。
- 源码、IL、原型和运行证据必须分开标记；调研完成不授权生产编码，而是输入 `/to-tickets` 和后续实施规格。

## 测试决策

人工开发者已明确确认三层最高测试 seam：共享契约行为、Gemini 前端 U3-SDK adapter 行为、GPT 后端 U3-SDK adapter 行为。因此本规格状态为 `ready-for-agent`。

- 测试使用最高可用 seam，包括共享契约行为、evaluator 输出、lifecycle transition、settings transaction、negotiation result 和 native-adapter observable behavior；不断言私有 helper、具体 collection、反射顺序或偶然 Harmony 细节。
- 共享契约编译测试同时使用客户端与 U3DS 兼容 reference 边界，并拒绝 UI/native 类型泄漏。
- 生命周期覆盖全部合法/非法转换、准入批处理、设置 bootstrap 顺序、首次异常隔离、依赖级联和幂等清理。
- 设置覆盖原子多字段提交、revision 冲突、RequestId replay、迁移失败、损坏持久化、服务器政策 overlay、session generation 失效。
- 网络覆盖截断/超长 payload、未知消息、nonce/generation 不匹配、不完整快照、重连和静默无插件。
- 候选覆盖 1×1、1×4、1×5、2×3、3×3、拥挤网格、边缘 clamp、current-local 稳定性和局部自动旋转。
- 坐标覆盖四角、四边中点、中心和小数抓取点，以及 0→1→2→3→0；四次正转必须在明确浮点容差内恢复原 footprint 和 offset。
- 测试必须明确区分 `grabOffsetInFootprint` 的闭区间行为和 intended center 的容器半开区间行为。
- 前端 adapter 测试 UI Scale、滚动、裁剪、输入焦点、view close、功能隔离和过期 generation callback。
- 后端 adapter 验证非法输入绝不到达 mutation、原生验证保持权威、插件不执行平行 remove/add 事务。
- 研究事实必须包含固定源码身份和证据类别；运行资格最终要求同一 DLL SHA-256 的 SP、P2P Host/Client 和 U3DS 独立证据。
- GPT-12 Node 8/8 仅是算法行为原型证据，不证明生产零分配、UI 集成或运行权威。

## 不在范围内

- 修改、替换或分发 Unturned 客户端/U3DS 原版文件或内容。插件只通过批准的 BepInEx、Harmony、公共契约和原生游戏 seam 集成。
- 二进制逆向、保护绕过、漏洞利用、未授权访问或滥用。允许读取已获授权的本地 U3-SDK/源码 reference 以记录兼容 adapter seam，但必须遵守本规格的证据和权限边界。
- 本阶段的生产实现、超出规格产物的工程脚手架、DLL 构建或打包。
- V1 动态加载第三方功能 DLL；面向外部 mod 的公开运行时 UI ABI。
- 用 LMN 替换 Unturned 库存 RPC；插件自管回滚、乐观库存修改或第二库存数据库。
- 自动交换、自动重排或库存排序。
- 在 U3-SDK 调研验收前冻结具体 Harmony target。
- 未经 Release C# allocation 测量就宣称零 GC；仅凭 `Application.isBatchMode` 宣称 U3DS 安全。
- 把源码、原型或构建证据当作 SP/P2P/U3DS 运行 PASS。
- 分配 Stable/1.0.0 成熟度或发布 Release。

## 补充说明

- 本规格已经可以分解 ticket 并由 Agent 开展 U3-SDK 调研，但尚未授权生产编码。
- `/to-tickets` 至少应生成共享契约基线、Gemini 前端调研、GPT 后端调研三个工作包；确认共享契约基线后，前后端调研可以并行。
- 调研中发现的共享契约变化必须保留单一定义产物模型和原生库存权威边界。
- 首个生产 CandidateBuild 必须保持预发布，不能继承原型或其他 DLL 哈希的运行证据。
