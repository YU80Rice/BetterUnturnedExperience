# GPT-Backend-Architecture-Spec：后端架构规格书

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT/Gemini Draft interface 基线已对齐；非 Stable；实现与三环境运行未验证**  
**唯一决策地图:** `map.md`

## 1. 架构目标

后端把定义产物验证与运行准入、生命周期、设置权威、兼容协商、候选计算和原生库存接入隐藏在小 interface 后面。前端只消费共享 DTO、静态消费 facet 和运行时状态投影；第三方模块不能直接依赖后端内部类。

## 2. 模块与依赖方向

```text
BetterUnturnedExperience.Contracts
          ↑
BetterUnturnedExperience.Core
          ↑
Feature-owned shared source manifests
          ↑
BetterUnturnedExperience.Aggregate (唯一 BepInPlugin / 单 DLL)
```

- 每个功能保留独立 `csproj` 与自有源码清单。
- 聚合工程一次编译全部选中源码，输出单一 DLL。
- 只有聚合工程声明唯一 `[BepInPlugin]`。
- UI 源码用编译条件或 adapter 隔离，U3DS 核心路径不引用 Glazier/Sleek 类型。
- ILMerge 不进入首版；ILRepack 仅保留为未来受控实验。

## 3. 核心深模块

### 3.1 ModuleRuntime

interface：接收 Runtime Feature Admission 的判别联合；对 `CoreEscalation` 直接进入核心 SafeMode，对可信 `AdmissionEvaluationBatch` 原子建立全部功能状态记录、保存 admitted handles、启动当前启用模块，并支持查询、隔离与停止。

完整状态机、依赖级联、资源登记与前端投影见 `Module-Lifecycle-Isolation-Spec.md`；该文件是 GPT-09 的唯一详细决策源。

implementation：

- 不重新解析 `feature.json`、目录、入口类型或身份规则；这些事实来自已验证的单一定义产物和 Runtime Feature Admission。
- 消费 Lifecycle facet 中已裁决的 required dependency DAG；实现仍防御性检查内部 handle 与拓扑不变量，但不建立第二套身份/关系解释器。
- 独占 `FeatureState`、`StateRevision` 与 `FeatureStatusChangedEvent` 写入权；Admission 只给出静态 decision。可信批次内全部功能先成为 `Discovered`：`Admit(handle)` 只保存 handle，`RejectIncompatible` 进入 `Incompatible` 且无 handle。SettingsRuntime 完成迁移/校验并提供 enablement snapshot 后，ModuleRuntime 才按动态设置、依赖和政策进入 `Starting/Disabled`。CoreEscalation 分支不得建立功能记录。
- 每个生命周期回调独立异常边界。
- 状态只能沿合法状态机转换。
- 模块失败时生成 `DiagnosticId`、记录版本和异常，投射 `FeatureStatusChangedEvent`。
- 共享契约或核心运行时自检失败时停止整体加载；普通功能失败只隔离该功能。

禁止宣称热卸载安全，除非对应 Harmony、事件、协程和 UI 资源均有对称释放证据。

### 3.2 SettingsRuntime

interface：读取不可变功能快照、原子提交一组同功能变更、订阅 revision 变化。静态设置描述由构建期 Settings facet 提供给统一设置外壳，不经 SettingsRuntime 动态声明；完整决策见 `Settings-Model-Authority-Spec.md`。

implementation：

- 权威固定为 `ClientLocal / ServerAuthoritative / ServerPolicyWithClientPreference`；模块不得扩展权威类型。
- revision 以 `FeatureId + SettingRevisionScope` 为粒度；一次同作用域多字段提交全部成功或全部失败，拒绝时返回完整当前快照。
- 服务器使用 `ExpectedRevision` 防止旧 UI 覆盖新值，并在单连接 generation 内对 `RequestId` 幂等去重。
- 每个功能、revision 作用域和权威存储作用域使用单写者主线程事务执行器；持久替换成功后才发布内存快照，崩溃恢复以磁盘 revision 为准。
- 客户端服务器快照是会话覆盖，不回写本地偏好；断线、换服或 generation 改变时清除。
- 写盘采用临时文件、flush、可重读校验和同卷原子替换；启动时校验 Schema，显式逐版本迁移，损坏文件保留为诊断证据。
- 不持久化库存状态，也不建立外部数据库。

### 3.3 PlacementCandidateEngine

interface：输入拖拽快照、容器几何、前端已换算的预期物品中心网格坐标和旋转偏好，返回一个 `ItemPlacementPreview`。

implementation 不变量：

1. 局部投影能放当前方向时立即保持当前方向，不检查旋转。
2. 局部当前方向失败而局部旋转方向可放时，就地自动旋转。
3. 两种局部投影都失败后，扩大搜索先当前方向、后旋转方向。
4. 同方向按候选中心到光标的平方欧氏距离排序，距离相同按 Y、再按 X。
5. 候选必须完整处于边界内且本地快照无占用。
6. 不自动交换、不重排；无候选时原物品位置不变。
7. 预览只是客户端建议，提交后服务器重新校验。

`PlacementCandidateInput.CursorGridX/Y` 是历史命名，实际承载 intended item center。原始 pointer 与 grab offset 的换算属于 ClientUi 坐标 adapter；PlacementCandidateEngine 不读取 UI Scale、像素坐标或抓取锚点。

该引擎应为无 Unity 副作用、生产热路径零分配的纯计算模块。中心投影、Local-Fit Priority、全容器扩大搜索与失败原因已由 `Item-Placement-Algorithm-Spec.md` 冻结；仍需生产 C# 分配测试和真实游戏手感验证。

### 3.4 VanillaInventoryAdapter

interface：获取只读拖拽快照、提交已选候选、订阅权威库存投影变化。

implementation：

- 客户端挂接 UI 候选计算和绿色/无效预览所需的只读数据。
- 释放时调用原版 `sendDragItem(page, x, y, rot)`。
- 服务端保留 `ReceiveDragItem` 的所有权、频率、页、坐标、容量、占用、资产和装备槽复验。
- 不直接写 `Items`。
- 不在 `removeItem` 与 `addItem` 之间插入插件逻辑。
- 不用 Harmony prefix 返回 `false` 绕过原版权威校验。

Harmony patch 只用于建立 seam：采集只读上下文、替换前端候选选择或观察结果。确切目标、签名和优先级必须由固定 U3-SDK commit 和运行时 IL/日志再次确认。

### 3.5 NetworkCapabilityAdapter

interface：在 LMN 频道协商后执行应用层 Hello/Snapshot/Ack/Ready，交换能力、同步服务器权威设置并投影当前客户端可见的模块状态。完整决策见 `Network-Capability-Versioning-Spec.md`。

implementation：使用 LMN V5 命名频道 adapter；网络 handler 验证后只入队，游戏状态变更在主线程执行。

限制：

- LMN 不是认证、授权、服务器发现或完整协议生命周期。
- LMN 命名频道握手不能替代框架的契约、功能与能力协商。
- payload 不能自报可信 SteamID。
- 每连接、每频道配置令牌桶限流和最大消息长度。
- 每次连接使用新的 generation 与 128-bit nonce；旧代际消息、快照和缓存全部失效。
- 应用握手 Ready 前不发送设置或状态投影；超时只禁用插件网络功能，不阻断原版连接。
- 未知频道/消息丢弃并限频记录。
- V1 不通过 LMN 重写库存拖放提交。

## 4. 线程与并发模型

- Unity/Unturned/Harmony 状态变更仅在已断言的游戏线程执行。
- 网络回调只做长度、版本、发送者上下文和字段范围校验，然后入有界队列。
- 候选纯计算可以使用不可变快照；发布快照时一次替换引用，不共享可变 `Items`。
- 每玩家拖拽预览不经过网络；服务器权威设置 command 采用每连接限流。
- 库存提交不添加插件级互斥锁。原版服务端方法在游戏线程重新校验；锁不能把客户端候选变成授权，也不能为 `remove/add` 提供安全回滚。
- 对设置和框架状态，使用 revision/状态机防重入；禁止跨线程持锁调用 Unity、LMN handler 或第三方模块回调。

## 5. 防刷与输入验证

- 原版拖放继续使用 `ONLY_FROM_OWNER` 与原生频率限制。
- LMN 设置消息独立使用每连接、每频道限流；超限只拒绝该消息并记录聚合诊断。
- 所有集合长度、字符串长度、枚举范围、revision 和 payload 长度先验证后分配。
- 权威 handler 从连接上下文识别玩家；客户端提供的身份字段只作显示信息时也必须忽略其授权意义。
- 模块异常不能中断其他模块 handler；首次未处理异常越过模块边界即按 GPT-09 转换为 `Isolating`。

## 6. 故障隔离

| 故障 | 行为 | 前端投影 |
| --- | --- | --- |
| 功能版本不兼容 | 不启动该功能 | `Incompatible` + 版本错误码 |
| 功能 Start 抛异常 | 隔离功能 | `Isolated` + DiagnosticId |
| 功能运行回调首次有未处理异常越过模块边界 | 注销该功能回调并隔离 | 启动失败/运行故障提醒 |
| UI adapter 在 U3DS 缺失 | 视为预期能力缺失 | 服务端不显示 UI 错误 |
| LMN 不可用或版本不匹配 | 禁用需要网络能力的功能 | `FeatureUnavailable` |
| Contracts/Core 自检失败 | 阻止整体插件加载 | 核心失败页或日志 |

状态转换和首次异常隔离规则已由 GPT-09 冻结；实现必须遵循 `Module-Lifecycle-Isolation-Spec.md`，不得在此处另建第二套阈值。

## 7. 日志与可观测性

每条关键日志至少包含：

- plugin/contract version
- feature id/version
- environment role（SP、P2P Host、P2P Client、U3DS）
- diagnostic id
- message kind/channel（如适用）
- 不含敏感 payload 的结构化原因码

日志不得把 `FeatureUnavailable` 写成“已修复”，也不得把构建/注册成功写成运行验收成功。

## 8. 持久化

- 链接功能定义、身份、入口与设置 Schema 属于嵌入 DLL 的单一定义产物，不作为运行期可变配置另行持久化。
- 本地/服务器设置值按功能、revision 作用域和权威存储作用域分文件保存，避免作用域互相覆盖。
- 配置路径由部署 adapter 提供，核心不硬编码游戏绝对路径。
- 每个设置文件包含格式标识、Schema version、feature version、持久化 revision、值集合和完整性校验信息。
- 迁移失败时保留旧文件、回退默认值、禁用受影响功能并提示；只有持久化根或核心一致性不变量损坏才进入 SafeMode。
- 不对玩家库存建立第二份持久化副本。

## 9. 前端交接时序

```text
Definition Artifact 格式、摘要与程序集绑定自检
  → Runtime Feature Admission 产出 CoreEscalation 或可信 AdmissionEvaluationBatch
  → CoreEscalation：Core SafeMode，不建立功能记录
  → 可信批次：ModuleRuntime 建立 Discovered，保存 Admit handles，投影 RejectIncompatible
  → SettingsRuntime 按 Settings facet 完成迁移/校验并产出 enablement snapshot
  → ModuleRuntime 结合动态依赖/政策唯一投影 Starting/Disabled
  → UI adapter 可用时发布 FeatureStatusView
  → Gemini 创建统一设置外壳与功能 View
  → 拖拽输入形成只读快照
  → PlacementCandidateEngine 返回预览
  → Gemini 渲染并在释放时调用 VanillaInventoryAdapter
  → 原版服务器重新校验和投影库存变化
  → Gemini 根据投影/超时策略结束视觉状态
```

前端不得等待一个当前不存在的逐次 LMN 成功 ACK 才恢复原版库存 UI。

稳定交互状态使用 `Idle → Dragging → Hovering → Dropping → AwaitingProjection → Idle`。每次新拖拽生成新的 `DragGeneration`；库存/容器关闭、功能隔离或新拖拽使旧 generation 失效。视觉超时只结束前端等待状态，不执行库存回滚或第二次提交。

## 10. 安全 Harmony 策略

允许的 patch 目的：

- 捕获拖拽开始、移动、旋转和释放时机。
- 替换或增强客户端候选位置选择。
- 挂载/卸载 UI adapter。
- 观察原版库存投影用于结束前端视觉状态。

禁止的 patch 目的：

- 跳过 `ReceiveDragItem` 校验。
- 客户端直接 `removeItem/addItem`。
- 在服务端 `removeItem/addItem` 两步之间执行插件回调。
- 用 patch 注册成功代替 SP/P2P/U3DS 运行证据。

## 11. 测试与发布门禁

静态与自动化：

- Contracts 序列化兼容测试。
- PlacementCandidateEngine 表驱动/属性测试。
- ModuleRuntime 状态机、异常隔离和重复 id 测试。
- SettingsRuntime revision、损坏文件和原子写测试。
- LMN adapter 截断、超长、未知消息、限流和线程入队测试。
- 客户端/U3DS 双程序集编译检查。

运行验收：

- 单人：背包和容器的预览、旋转、提交失败保留。
- SteamP2PFriends：Host/Client 同哈希，服务端复验、设置同步和失败隔离。
- U3DS：客户端/服务器同哈希，UI 无服务端硬依赖，设置与能力同步。
- 每次 DLL 或源码变化都需要新的共享 Case ID、版本、SHA-256 和双端日志；旧哈希证据不继承。

## 12. 已冻结的后续决策来源

- GPT-09：已冻结模块生命周期、首次异常隔离、依赖级联和资源清理义务。
- GPT-10：已冻结三种设置权威、每功能/作用域原子 revision、持久化/迁移和前端快照消费契约。
- GPT-11：已冻结双层握手、版本/能力降级、原子分片和状态投影范围。
- GPT-12：已冻结 Local-Fit Priority；原型与协作者测试只构成 Wayfinder 行为证据，生产 C#、零分配和游戏手感仍待实现阶段验证。
- GPT-13 已冻结前端在无逐次 ACK 条件下的失败/超时表现；实现须遵循 `Frontend-Backend-Handoff-Spec.md`。
- GPT-15：已完成事后精确复核；Gemini 接受当前 DTO、状态、设置和权威边界，候选 UI 细节继续归入 GPT-09/12/13。
- GPT-14：已冻结领域片段、Definition Linker、单一定义产物、Runtime Feature Admission、CandidateBuild、资格证据和发布授权；本总纲不得恢复运行时 Describe、原始 manifest 解析或平行 Catalog/Gate/Permit 链。

