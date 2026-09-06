# GPT-Shared-Contract-Spec：共享契约规格书

**作者: GPT**  
**版本: 0.1.0-draft**  
**状态: GPT/Gemini Draft interface 基线已对齐；非 Stable；实现与运行未验证**  
**唯一决策地图:** `map.md`

## 1. 目的与证据边界

本规格定义前端、后端、核心运行时和功能模块共同依赖的最小 interface。它落实 GPT-08；Gemini 已在 `to-post-contract-review.md` 中逐项接受当前 DTO、状态、设置线路和权威边界。该接受只证明前端可消费性与 Draft 接口一致，不证明编译、实现、Stable ABI 或三环境运行。Gemini 草案中的具体 UI 类名、LMN 库存消息和动画时序仍不自动升级为公共契约。

已确认事实：

- 库存权威提交必须保留原版 `sendDragItem → ReceiveDragItem` 路径。
- 单人和 SteamP2PFriends 房主走服务端 loopback；P2P 客机和 U3DS 客户端请求远端服务端。
- 客户端候选结果不具有授权性，服务器必须重新校验。
- LaunchMultiplayerNet（LMN）是应用层消息总线，不是身份认证、授权、事务或库存权威系统。
- 客户端与 U3DS 的 BepInEx、`Assembly-CSharp.dll` 基线不同，公共契约不得依赖 UI 类型或单端独有类型。

未完成：新框架尚无 SP、SteamP2PFriends、U3DS 同哈希运行证据。

## 2. 稳定 interface 设计

### 2.1 标识与版本

```csharp
public readonly struct FeatureId
{
    public string Value { get; }
}

public readonly struct ContractVersion
{
    public ushort Major { get; }
    public ushort Minor { get; }
}

public readonly struct FeatureDependency
{
    public FeatureId Feature { get; }
    public Version MinimumFeatureVersion { get; }
    public ContractVersion MinimumContract { get; }
    public bool Required { get; }
}

public readonly struct FeatureScopeIdentity
{
    public FeatureId Id { get; }
    public Version FeatureVersion { get; }
    public string CurrentSlug { get; }
    public string DefinitionSetId { get; }
    public Digest256 DefinitionSetDigest { get; }
}

public readonly struct Digest256
{
    public ulong Part0 { get; }
    public ulong Part1 { get; }
    public ulong Part2 { get; }
    public ulong Part3 { get; }
}

public enum FeatureStopReason : byte
{
    None,
    PluginStopping,
    UserDisabled,
    VersionIncompatible,
    RuntimeIsolated,
    EnvironmentUnavailable,
    DependencyUnavailable,
    CoreSafeMode
}

public readonly struct FeatureStartResult
{
    public bool Started { get; }
    public FrameworkErrorCode Error { get; }
    public string DiagnosticId { get; }
}
```

规则：

- `FeatureId` 使用稳定的小写反向域名或仓库约定标识，不使用本地化显示名作为身份。
- `DisplayNameKey` 是本地化键，不是协议身份。
- Major 不一致视为不兼容；同 Major 下较高 Minor 只能追加可忽略字段或能力。
- 线路消息必须同时带 `contractMajor`、`contractMinor`、`messageKind` 和 `payloadLength`。

`FeatureDependency` 是领域 Dependency fragment 与生成 facet 使用的逻辑共享类型，不是模块在 `Start` 时提交的运行时声明，也不允许 Runtime Lifecycle 重新解释原始 manifest。运行时模块只能通过已链接的本地 dependency id 使用 `IDependencyCapabilityView`。

### 2.2 模块 interface

```csharp
public interface IFeatureModule
{
    FeatureStartResult Start(IFeatureBootstrap bootstrap);
    void Stop(FeatureStopReason reason);
}

public interface IFeatureBootstrap
{
    FeatureScopeIdentity Identity { get; }
    ulong LifecycleGeneration { get; }
    IScopedFeatureSettings Settings { get; }
    IFeatureEventSubscriber Events { get; }
    IOwnedFeatureEventPublisher OwnedEvents { get; }
    IFeatureLogger Logger { get; }
    IDependencyCapabilityView Dependencies { get; }
    IFeatureLifetime Lifetime { get; }
}

public interface IFeatureLifetime
{
    bool TryTrack(IDisposable registration);
}

public interface IScopedFeatureSettings
{
    FeatureSettingsSnapshot GetSnapshot(SettingRevisionScope revisionScope);
    bool TryGet(string settingId, out SettingValue value, out uint revision);
    SettingChangeResult Submit(ScopedSettingChangeRequest request);
}

public interface IFeatureEventSubscriber
{
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);
}

public interface IOwnedFeatureEventPublisher
{
    bool TryPublish<TEvent>(string declaredEventId, TEvent value);
}

public interface IFeatureLogger
{
    void Info(string eventName, string diagnosticId);
    void Warning(string eventName, FrameworkErrorCode error, string diagnosticId);
    void Error(string eventName, FrameworkErrorCode error, string diagnosticId, Exception exception);
}

public interface IDependencyCapabilityView
{
    bool Has(string declaredDependencyId, string capabilityId, ushort minimumVersion);
    bool TryGet(string declaredDependencyId, out NegotiatedFeatureView feature);
}
```

`IReadOnlyList<T>`、`IDisposable`、`Action<T>` 与 `Exception` 来自 .NET Framework 4.7.2 基础库。该 interface 不暴露 BepInEx、Harmony、Glazier、Steamworks、LMN 或 Unturned 具体类型。对应实现通过内部 adapter 提供，避免 U3DS 被 UI 类型硬依赖。

`IFeatureModule` 不再提供 `Describe()`；FeatureId、版本、依赖、设置 schema、能力声明和入口绑定来自 GPT-14 的链接功能定义包。`IFeatureBootstrap` 由核心在 Runtime Feature Admission 后按当前功能作用域创建，模块可读取自身身份但不能选择或改写身份。`IScopedFeatureSettings` 不接受任意 FeatureId；跨进程/统一设置 UI 仍使用带 FeatureId 的路由 command。

事件订阅与发布使用不同 interface。`IFeatureEventSubscriber` 只能订阅当前功能的链接事件消费 facet 所允许的事件；`IOwnedFeatureEventPublisher` 只接受当前功能 Event fragment 中预先声明的 `declaredEventId`，并必须验证该 id 声明的事件类型与 `TEvent` 完全匹配。未知、未拥有或类型不匹配返回 false 并记录诊断。泛型类型本身不授予发布权。

`IDependencyCapabilityView` 使用当前功能 Dependency fragment 中的稳定本地 dependency id，而不是任意 FeatureId。未声明 dependency id 永远不可见；实现还必须在每次调用时检查依赖状态、能力协商与 lifecycle generation。

### 2.3 功能状态投影

```csharp
public enum FeatureState : byte
{
    Discovered,
    Incompatible,
    Disabled,
    Starting,
    Running,
    Isolating,
    Isolated,
    Stopping,
    Stopped
}

public readonly struct FeatureStatusView
{
    public FeatureId Feature { get; }
    public FeatureState State { get; }
    public FrameworkErrorCode Error { get; }
    public FeatureStopReason StopReason { get; }
    public string DiagnosticId { get; }
    public ulong StateRevision { get; }
}
```

`DiagnosticId` 用于日志关联；前端显示本地化错误文案，不直接展示异常堆栈。

生命周期、合法转换、依赖级联、revision 与清理义务见 `Module-Lifecycle-Isolation-Spec.md`。

## 3. “更好的物品交互”共享数据

### 3.1 候选位置

```csharp
public readonly struct ItemGridPosition
{
    public byte Page { get; }
    public byte X { get; }
    public byte Y { get; }
    public byte Rotation { get; }
}

public readonly struct ContainerReference
{
    public ContainerKind Kind { get; }
    public byte Page { get; }
    public uint SessionGeneration { get; }
}

public enum ContainerKind : byte
{
    PlayerInventory,
    Storage,
    Equipment
}
```

`ContainerReference` 不携带客户端可伪造的“已授权”布尔值。服务器按当前玩家状态和已打开容器重新解析；`SessionGeneration` 只用于拒绝过期 UI 上下文，不授予访问权。

### 3.2 本地候选意图

```csharp
public readonly struct ItemPlacementIntent
{
    public uint DragGeneration { get; }
    public ItemGridPosition Source { get; }
    public ContainerReference TargetContainer { get; }
    public ItemGridPosition Candidate { get; }
}

public readonly struct ItemPlacementPreview
{
    public uint DragGeneration { get; }
    public PlacementPreviewState State { get; }
    public ItemGridPosition Candidate { get; }
    public byte Width { get; }
    public byte Height { get; }
    public PlacementReason Reason { get; }
}

public readonly struct PlacementCandidateInput
{
    public uint DragGeneration { get; }
    public ItemGridPosition Source { get; }
    public ContainerReference TargetContainer { get; }
    public float CursorGridX { get; }
    public float CursorGridY { get; }
    public byte ItemWidth { get; }
    public byte ItemHeight { get; }
    public byte CurrentRotation { get; }
    public bool AllowAutomaticRotation { get; }
    public IGridOccupancyView Occupancy { get; }
}

public interface IGridOccupancyView
{
    byte Width { get; }
    byte Height { get; }
    bool IsOccupied(byte x, byte y);
}

public interface IPlacementCandidateEvaluator
{
    ItemPlacementPreview Evaluate(PlacementCandidateInput input);
}
```

`ItemPlacementIntent` 与 `ItemPlacementPreview` 是进程内 interface。V1 中它们不替代原版网络 RPC，也不证明服务器接受。

`CursorGridX/Y` 是 Draft interface 保留的历史字段名，其冻结语义不是“原始屏幕鼠标位置”，而是前端 Presenter 已完成抓取偏移换算后的 **intended item center grid**。调用方必须先执行：

```text
intendedItemCenterGrid = pointerGrid + (currentFootprintCenter - grabOffsetInFootprint)
```

再把结果写入 `CursorGridX/Y`。`grabOffsetInFootprint`、像素坐标、UI Scale 与旋转视觉锚点均属于 ClientUi 坐标 adapter，不进入 evaluator interface。

坐标约定冻结为：容器/footprint 局部原点在左上，`+X` 向右、`+Y` 向下；grab offset 是从当前朝向 footprint 左上到 pointer 的连续坐标，数学合法域为闭区间 `[0,W] × [0,H]`，不是离散格索引，因此公式没有 `-1`。这不改变 evaluator 中心坐标的容器有效域：`CursorGridX/Y`（承载 intended item center）使用 `[0,containerWidth) × [0,containerHeight)`，越界返回 `Hidden/OutsideGrid`。Unturned 原生 forward quarter-turn 是 `(rotation + 1) & 3`。对当前 footprint `W × H`：

```text
Forward/native rot+1:  (gx, gy) -> (H - gy, gx)   ; new footprint H × W
Backward/native rot-1: (gx, gy) -> (gy, W - gx)   ; new footprint H × W
```

forward 变换的校验点：`(0,0)->(H,0)`、`(W,0)->(H,W)`、`(0,H)->(0,0)`、`(W,H)->(0,W)`、中心 `(W/2,H/2)->(H/2,W/2)`。该方向由冻结源码 `PlayerDashboardInventoryUI.updateDraggedItem()` 的 `dragJar.rot++` 与 `updatePivot()` 共同确认。前端每次 forward 旋转后先变换 grab offset，再重算 intended center。生产命名可在首次 Stable ABI 前改为 `IntendedCenterGridX/Y`；若保留旧名，以上语义不可改变。

`IPlacementCandidateEvaluator` 必须无状态、无 Unity 副作用且不持有 `IGridOccupancyView`。实现可以作为单例复用；调用方负责提供稳定的只读快照。该 seam 回答 Gemini 的阻塞问题：前端不维护私有吸附算法，只消费 GPT-12 最终实现。

GPT-12 的候选优先级冻结为 Local-Fit Priority：局部当前方向 → 局部旋转方向 → 扩大搜索当前方向 → 扩大搜索旋转方向。完整投影公式、Reason 与零分配门禁见 `Item-Placement-Algorithm-Spec.md`。Occupancy 快照必须排除当前拖拽物品自身 footprint。

### 3.3 预览状态与原因

```csharp
public enum PlacementPreviewState : byte
{
    Hidden,
    Candidate,
    LocallyInvalid,
    PendingAuthoritativeProjection
}

public enum PlacementReason : ushort
{
    None = 0,
    OutsideGrid = 100,
    Occupied = 101,
    UnsupportedContainer = 102,
    StaleDrag = 103,
    NoCandidate = 104,
    ServerStateChanged = 200,
    ServerRejected = 201,
    FeatureUnavailable = 300,
    VersionMismatch = 301,
    RateLimited = 302,
    InternalFailure = 900
}
```

原因码是稳定机器身份；显示文本由前端本地化。未知码必须降级为通用失败，不得崩溃。

### 3.4 前端交互状态 seam

```csharp
public enum DragInteractionState : byte
{
    Idle,
    Dragging,
    Hovering,
    Dropping,
    AwaitingProjection
}

public readonly struct DragInteractionView
{
    public uint DragGeneration { get; }
    public DragInteractionState State { get; }
    public ItemPlacementPreview Preview { get; }
}
```

| 当前状态 | 输入/事实 | 下一状态 | 契约效果 |
| --- | --- | --- | --- |
| Idle | 原版确认抓取物品 | Dragging | `DragGeneration` 加一，冻结来源快照 |
| Dragging | 光标进入受支持网格 | Hovering | 调用 evaluator，发布当前 generation 的 preview |
| Hovering | 光标移动或旋转输入 | Hovering | 重算 preview；旧 generation 结果丢弃 |
| Hovering | 光标离开网格 | Dragging | 隐藏 preview，不改变库存 |
| Dragging/Hovering | 释放且无合法候选 | Idle | 取消增强提交，保留原位置并使 generation 失效 |
| Hovering | 释放且有合法候选 | Dropping | 锁定候选元组并调用原版 `sendDragItem` adapter |
| Dropping | 原版调用已发出或 loopback 已进入 | AwaitingProjection | 清理拖拽图层，不做乐观库存修改 |
| AwaitingProjection | 原版库存投影变化、上下文关闭、功能禁用或本地观察超时 | Idle | 结束本 generation；超时只影响视觉，不回滚权威数据 |
| AwaitingProjection | 玩家开始新的原版拖拽 | Dragging | 旧 generation 立即失效，创建新的非零 generation |

规则：

- 每次进入新 Dragging 都生成非零且不同的 `DragGeneration`；关闭库存、切换角色、模块隔离或新拖拽开始都会使旧 generation 失效。
- 只有与当前 generation 相符的候选结果能更新 UI。
- 视觉颜色、缓动和超时秒数由 Gemini/GPT-13 原型决定；状态含义和库存零乐观写入是稳定契约。

## 4. Command / Event 裁定

### 4.1 V1 已批准

```csharp
public readonly struct UpdateModuleConfigCommand
{
    public ulong RequestId { get; }
    public FeatureId Feature { get; }
    public SettingRevisionScope RevisionScope { get; }
    public uint ExpectedRevision { get; }
    public IReadOnlyList<SettingMutation> Mutations { get; }
}

public readonly struct ScopedSettingChangeRequest
{
    public ulong RequestId { get; }
    public SettingRevisionScope RevisionScope { get; }
    public uint ExpectedRevision { get; }
    public IReadOnlyList<SettingMutation> Mutations { get; }
}

public readonly struct RequestModuleConfigSnapshotCommand
{
    public ulong RequestId { get; }
    public FeatureId Feature { get; }
    public SettingRevisionScope RevisionScope { get; }
    public uint KnownRevision { get; }
}

public readonly struct SettingMutation
{
    public string SettingId { get; }
    public SettingValue Value { get; }
}

public readonly struct ModuleConfigChangedEvent
{
    public ulong RequestId { get; }
    public FeatureId Feature { get; }
    public SettingRevisionScope RevisionScope { get; }
    public uint Revision { get; }
    public FeatureSettingsSnapshot Snapshot { get; }
}

public readonly struct ModuleConfigRejectedEvent
{
    public ulong RequestId { get; }
    public FeatureId Feature { get; }
    public SettingRevisionScope RevisionScope { get; }
    public FrameworkErrorCode Error { get; }
    public uint CurrentRevision { get; }
    public FeatureSettingsSnapshot Snapshot { get; }
}

public readonly struct FeatureStatusChangedEvent
{
    public FeatureStatusView Status { get; }
}

public enum CoreRuntimeState : byte
{
    Initializing,
    Running,
    SafeMode,
    Stopping,
    Stopped
}

public readonly struct CoreRuntimeStatusView
{
    public CoreRuntimeState State { get; }
    public FrameworkErrorCode Error { get; }
    public string DiagnosticId { get; }
    public ulong StateRevision { get; }
}

public readonly struct CoreRuntimeStatusChangedEvent
{
    public CoreRuntimeStatusView Status { get; }
}

public enum SettingKind : byte
{
    Toggle,
    Integer,
    Float,
    Text,
    KeyBinding,
    Choice
}

public enum SettingAuthority : byte
{
    ClientLocal,
    ServerAuthoritative,
    ServerPolicyWithClientPreference
}

public enum SettingRevisionScope : byte
{
    ClientPreference,
    ServerAuthority
}

public readonly struct SettingValueOption
{
    public bool HasValue { get; }
    public SettingValue Value { get; }
}

public readonly struct SettingValue
{
    public SettingKind Kind { get; }
    public bool Boolean { get; }
    public int Integer { get; }
    public float Float { get; }
    public string Text { get; }
}

public readonly struct SettingDescriptor
{
    public FeatureId Feature { get; }
    public string SettingId { get; }
    public string DisplayNameKey { get; }
    public string DescriptionKey { get; }
    public SettingKind Kind { get; }
    public SettingAuthority Authority { get; }
    public SettingValue DefaultValue { get; }
    public SettingValueOption Minimum { get; }
    public SettingValueOption Maximum { get; }
    public SettingValueOption Step { get; }
    public IReadOnlyList<SettingValue> AllowedValues { get; }
    public ushort MaximumUtf8Bytes { get; }
    public string ValidationRuleId { get; }
    public uint SchemaVersion { get; }
    public int SortOrder { get; }
    public string VisibilityRuleId { get; }
    public string EnablementRuleId { get; }
}

public enum SettingSyncState : byte
{
    Ready,
    AwaitingAuthoritativeSnapshot,
    Unavailable
}

public enum SettingSnapshotSource : byte
{
    LocalPersistent,
    SingleplayerAuthority,
    P2PHostAuthority,
    DedicatedServerAuthority,
    SessionProjection,
    SafeDefault
}

public readonly struct SettingPolicyView
{
    public SettingValueOption Minimum { get; }
    public SettingValueOption Maximum { get; }
    public SettingValueOption Step { get; }
    public IReadOnlyList<SettingValue> AllowedValues { get; }
    public ushort MaximumUtf8Bytes { get; }
    public string ValidationRuleId { get; }
}

public readonly struct SettingEntryView
{
    public string SettingId { get; }
    public SettingAuthority Authority { get; }
    public SettingValueOption ClientPreference { get; }
    public bool HasPolicy { get; }
    public SettingPolicyView Policy { get; }
    public SettingValue EffectiveValue { get; }
    public bool IsVisible { get; }
    public bool CanEdit { get; }
}

public readonly struct FeatureSettingsSnapshot
{
    public FeatureId Feature { get; }
    public uint SchemaVersion { get; }
    public SettingRevisionScope RevisionScope { get; }
    public uint Revision { get; }
    public SettingSyncState SyncState { get; }
    public SettingSnapshotSource Source { get; }
    public IReadOnlyList<SettingEntryView> Entries { get; }
}

public readonly struct SettingChangeResult
{
    public bool Accepted { get; }
    public FrameworkErrorCode Error { get; }
    public uint Revision { get; }
    public FeatureSettingsSnapshot Snapshot { get; }
}
```

- 本地视觉设置可进程内提交并原子持久化；服务器快照只形成会话权威覆盖，不覆盖本地持久化偏好。
- 服务器权威设置通过 LMN adapter 发送。服务器校验后向请求方返回带原 `RequestId` 的 changed/rejected；若有效值需要通知其他观察者，则向其他连接发送 `RequestId = 0` 的 changed 广播。
- 前端不能以本地写入成功推断服务器已接受。
- `RequestId` 由请求方按连接会话单调生成，`0` 保留；服务器只在发给请求方的 changed/rejected 中原样回显，观察者广播固定为 `0`。它只做关联，不提供防重放授权。
- 连接重建后请求方从新的随机非零起点开始单调生成；服务器的重复请求缓存限定在单连接会话内。相同 `RequestId` 且 payload 相同可重放既有结果；相同 id 但 payload 不同必须以 `RequestIdConflict` 拒绝并记录诊断。
- 一次 command 只包含同一 FeatureId、同一 `SettingRevisionScope` 的有界 mutation 集合；它以该作用域为原子单位全部成功或全部失败。changed/rejected 均携带完整作用域快照，前端不得拼接部分权威状态。
- `Revision` 以 `FeatureId + SettingRevisionScope` 为粒度；无实际值变化、拒绝或重复请求重放不增加 revision。
- `ServerPolicyWithClientPreference` 的服务器政策与客户端偏好分别属于两个 revision 作用域；`SettingEntryView` 同时投影偏好、政策和推导后的有效值。
- `ClientPreference` command 是进程内提交，不通过 LMN；线路 `0x0101` 只允许 `RevisionScope = ServerAuthority`，且服务端仍按连接权限决定发送者是否可修改。
- 描述器适用性、持久化和同步语义详见 `Settings-Model-Authority-Spec.md`。
- `RequestModuleConfigSnapshotCommand` 只允许请求当前发送者可见的单个 FeatureId/RevisionScope 完整快照。服务器成功时以 `ModuleConfigChangedEvent` 回显同一 RequestId 和当前 revision；未知功能、未就绪或无权限时以 `ModuleConfigRejectedEvent` 回应。请求不得触发 revision 增长或设置写入。
- `CoreRuntimeStatusChangedEvent` 是进程内核心→本地前端事件，不注册为 V1 LMN 消息。其 revision 单调递增；客户端仅用它识别本进程核心 SafeMode，U3DS 消费日志而不实例化 UI。

### 4.2 V1 明确不作为网络权威协议

Gemini 提案中的以下名称保留为讨论词，但不注册为 V1 LMN 消息：

- `CommitItemPlacementCommand`
- `RequestRotateItemCommand`
- `ItemPlacementCommittedEvent`
- `ItemPlacementRejectedEvent`
- `InventoryStateUpdatedEvent`

理由：原版拖放已经存在所有权、限流和服务端复验路径；平行的自定义提交协议会重复权威逻辑并扩大物品丢失、复制和竞态风险。旋转是候选计算输入；最终提交复用 `(page, x, y, rot)` 调用原版 `sendDragItem`。UI 成功状态来自原版库存投影变化；超时或拒绝的表现策略留给 GPT-13，不承诺逐请求网络 ACK。

## 5. LaunchMultiplayerNet adapter

网络能力、版本和状态投影的完整裁定见 `Network-Capability-Versioning-Spec.md`。

### 5.0 能力 DTO

```csharp
public enum CapabilityDirection : byte { LocalOnly, ServerToClient, Bidirectional }
public enum CapabilityRequirement : byte { Optional, RequiredForFeature, RequiredForSession }

[Flags]
public enum CapabilityEnvironment : byte
{
    None = 0,
    Client = 1,
    SingleplayerAuthority = 2,
    P2PHostAuthority = 4,
    DedicatedServerAuthority = 8
}

public readonly struct CapabilityDescriptor
{
    public FeatureId Provider { get; }
    public string CapabilityId { get; }
    public ushort Version { get; }
    public CapabilityDirection Direction { get; }
    public CapabilityRequirement Requirement { get; }
    public CapabilityEnvironment Environments { get; }
    public ContractVersion RequiredContract { get; }
}

public readonly struct FeatureCapabilityManifest
{
    public FeatureId Feature { get; }
    public WireSemanticVersion FeatureVersion { get; }
    public IReadOnlyList<CapabilityDescriptor> Capabilities { get; }
}

public readonly struct WireSemanticVersion
{
    public ushort Major { get; }
    public ushort Minor { get; }
    public ushort Patch { get; }
}

public enum NegotiationState : byte { Pending, Available, Degraded, Incompatible, Unavailable }

public readonly struct NegotiatedFeatureView
{
    public FeatureId Feature { get; }
    public WireSemanticVersion LocalVersion { get; }
    public WireSemanticVersion RemoteVersion { get; }
    public NegotiationState State { get; }
    public FrameworkErrorCode Error { get; }
    public ulong NegotiationRevision { get; }
    public IReadOnlyList<CapabilityDescriptor> AcceptedCapabilities { get; }
}

public readonly struct ConnectionHandshakeId
{
    public ulong ConnectionGeneration { get; }
    public ulong NonceHigh { get; }
    public ulong NonceLow { get; }
}

public readonly struct CapabilityHello
{
    public ConnectionHandshakeId ClientHandshake { get; }
    public WireSemanticVersion FrameworkVersion { get; }
    public ContractVersion Contract { get; }
    public IReadOnlyList<FeatureCapabilityManifest> Features { get; }
}

public readonly struct CapabilitySnapshot
{
    public ulong ConnectionGeneration { get; }
    public ulong ClientNonceHigh { get; }
    public ulong ClientNonceLow { get; }
    public ulong ServerNonceHigh { get; }
    public ulong ServerNonceLow { get; }
    public ulong SnapshotId { get; }
    public WireSemanticVersion ServerFrameworkVersion { get; }
    public ContractVersion ServerContract { get; }
    public IReadOnlyList<NegotiatedFeatureView> Features { get; }
}

public readonly struct CapabilityAck
{
    public ulong ConnectionGeneration { get; }
    public ulong SnapshotId { get; }
    public ulong ServerNonceHigh { get; }
    public ulong ServerNonceLow { get; }
}

public readonly struct SessionReadyEvent
{
    public ulong ConnectionGeneration { get; }
    public ulong SnapshotId { get; }
}

public readonly struct HandshakeReject
{
    public ulong ConnectionGeneration { get; }
    public ulong ClientNonceHigh { get; }
    public ulong ClientNonceLow { get; }
    public FrameworkErrorCode Error { get; }
    public ushort SupportedContractMajor { get; }
}

public enum SnapshotKind : byte
{
    CapabilityHello,
    CapabilitySnapshot,
    SettingsSnapshot
}

public readonly struct SnapshotChunkEnvelope
{
    public ulong ConnectionGeneration { get; }
    public ulong SnapshotId { get; }
    public SnapshotKind Kind { get; }
    public ushort ChunkIndex { get; }
    public ushort ChunkCount { get; }
    public uint TotalLength { get; }
    public ushort ChunkLength { get; }
    public byte[] Sha256 { get; }
    public byte[] ChunkBytes { get; }
}
```

`RequiredForSession` 在 V1 不允许第三方模块启用。能力声明只参与兼容交集，不参与身份或权限判断。

### 5.1 命名频道

建议保留以下频道；最终名称须在实现前核对 LMN V5 规范：

| 频道 | 方向 | 可靠性 | 用途 |
| --- | --- | --- | --- |
| `betterunturned.core.capabilities` | 双向 | reliable | 合同版本、功能能力和兼容状态 |
| `betterunturned.core.settings` | 双向 | reliable | 服务器权威设置命令与快照 |
| `betterunturned.core.diagnostics` | server→client | reliable | 模块隔离、版本不兼容和诊断关联 id |

禁止把频道名当作认证或授权。服务器端处理器必须从 LMN/Unturned 连接上下文解析发送者身份，不接受 payload 自报 SteamID。

### 5.2 消息 kind 分配

| messageKind | 消息 | 方向 |
| --- | --- | --- |
| `0x0001` | CapabilityHello/Chunk | client→server |
| `0x0002` | CapabilitySnapshot/Chunk | server→client |
| `0x0003` | CapabilityAck | client→server |
| `0x0004` | SessionReadyEvent | server→client |
| `0x0101` | UpdateModuleConfigCommand | client→server |
| `0x0102` | ModuleConfigChangedEvent | server→client |
| `0x0103` | ModuleConfigRejectedEvent | server→client |
| `0x0104` | RequestModuleConfigSnapshotCommand | client→server |
| `0x0201` | FeatureStatusChangedEvent | server→client；仅当前客户端可见的服务器/协商状态 |

上述 Contract envelope 网络消息已在 GPT-11 的 Wayfinder 决策中批准语义，但仍是 Draft：生产实现、序列化测试和三环境运行未完成。设置与状态消息只能在应用握手进入 Ready 后发送。Contract Major 不匹配的 `HandshakeReject` 不占用普通 messageKind；它使用 GPT-11 冻结的独立 `BUEB` bootstrap frame，并在 capabilities 频道上先于普通 envelope 判别。

### 5.3 线路封装

```text
u16 contractMajor
u16 contractMinor
u16 messageKind
u32 payloadLength
bytes payload
```

`0x0001` 与 `0x0002` 的 payload 永远编码为 `SnapshotChunkEnvelope`，单片也不编码裸 `CapabilityHello/CapabilitySnapshot`。Envelope 的 `SnapshotKind` 必须分别为 CapabilityHello/CapabilitySnapshot，否则整份消息拒绝。

约束：

- 多字节整数固定 little-endian。
- 字符串为 UTF-8、长度前置、分别设上限；禁止无界字符串和集合。
- 解码先验证版本、类型、长度和剩余字节，再构造 DTO。
- LMN V2 单消息上限低于其 60 KiB 总负载边界，框架自身硬限制为 16 KiB；分片业务数据不超过 12 KiB，单快照不超过 512 KiB。
- handler 只完成验证和入队；Unity/Unturned 状态变更在已断言的游戏线程执行。
- 未知 `messageKind` 在同 Major 下丢弃并记录受限诊断；Major 不兼容时禁用对应功能。

### 5.4 设置消息字段编码

共同规则：`FeatureId`、`SettingId` 使用 `u16 byteLength + UTF-8 bytes`，分别限制 96 与 128 bytes；`RequestId` 为 `u64`；revision 为 `u32`；枚举为 `u8/u16`；`SettingValue` 为 `u8 kind` 后跟该 kind 的固定或有界值。

| kind | payload 字段顺序 |
| --- | --- |
| `0x0101` | requestId, featureId, revisionScope, expectedRevision, mutationCount, repeated(settingId, settingValue) |
| `0x0102` | requestId, featureId, revisionScope, revision, fullSnapshot |
| `0x0103` | requestId, featureId, revisionScope, errorCode, currentRevision, fullSnapshot |
| `0x0104` | requestId, featureId, revisionScope, knownRevision |

`mutationCount` 硬上限为 64，快照 entry count 硬上限为每功能 256；两者仍受 16 KiB 单消息和 512 KiB 单快照限制。先验证 count 再分配集合，解码时增量拒绝重复 SettingId，且在整条消息通过前不修改业务状态。`0x0104` 不含 mutation，必须使用区别于原设置提交的新非零 RequestId；`knownRevision` 只用于诊断与可选的同版本响应优化，服务器仍必须返回完整快照。每连接、每 FeatureId/RevisionScope 最多每 5 秒接受 1 个快照请求，允许初始突发 2 个，超额返回 `RateLimited`。`SettingValue.Text` 与 KeyBinding 文本最多 512 UTF-8 bytes。kind 与描述不匹配、数值越界或尾部截断时拒绝整条消息。decoder 规则如下：

- 消息 Minor 等于或低于 reader 支持的 Minor 时，必须精确消费该 Minor 定义的全部字段；出现未定义尾随字节则拒绝。
- 消息 Minor 高于 reader 支持的 Minor、Major 相同时，reader 精确读取自己已知的必需前缀，并按 `payloadLength` 跳过未知合法尾部。
- 截断任何已知必需字段一律拒绝。
- 新 Minor 只能在 payload 尾部追加可选字段，不能改变既有字段顺序、类型、单位或含义。

## 6. 错误码体系

```csharp
public enum FrameworkErrorCode : ushort
{
    None = 0,
    ContractMajorMismatch = 1000,
    ContractMinorUnsupported = 1001,
    CapabilityMissing = 1002,
    FeatureVersionMismatch = 1003,
    NetworkCapabilitiesUnavailable = 1004,
    HandshakeTimedOut = 1005,
    HandshakeNonceMismatch = 1006,
    StaleConnectionGeneration = 1007,
    SnapshotIncomplete = 1008,
    SnapshotIntegrityFailed = 1009,
    MalformedPayload = 1100,
    PayloadTooLarge = 1101,
    UnknownMessageKind = 1102,
    UnauthorizedSender = 1200,
    RateLimited = 1201,
    SettingRejected = 1300,
    SettingUnknown = 1301,
    SettingTypeMismatch = 1302,
    SettingValidationFailed = 1303,
    SettingRevisionConflict = 1304,
    RequestIdConflict = 1305,
    SettingSnapshotNotReady = 1306,
    SettingSchemaIncompatible = 1307,
    SettingPersistenceFailed = 1308,
    SettingMigrationFailed = 1309,
    ModuleStartFailed = 2000,
    ModuleRuntimeIsolated = 2001,
    DependencyUnavailable = 2002,
    InvalidStateTransition = 2003,
    DependencyCycle = 2004,
    DependencyVersionMismatch = 2005,
    CleanupIncomplete = 2006,
    CoreRuntimeFailure = 9000
}
```

错误码区段归 GPT 维护。第三方功能只能在其登记区段内扩展，不得复用核心码。

## 7. 序列化兼容规则

- 首版使用显式字段顺序和手写有界 reader/writer，不使用运行时反射序列化。
- 同 Major 的新增字段必须追加在 payload 尾部；只有更高 Minor 消息的未知尾部允许旧 reader 依据 payload 长度忽略。
- 字段删除、重排、意义改变或数值单位改变必须提升 Major。
- 枚举未知值必须安全降级；不得假定双方枚举集合完全相同。
- 任何预编译消费方在共享程序集 `AssemblyVersion` 主版本变化后必须重新编译。
- 契约测试必须覆盖截断、超长、未知类型、未知枚举、重复消息、乱序 revision 和跨 Minor 读取。

## 8. 保留为内部实现的内容

以下不进入公共 interface：

- Harmony patch 类型、方法名和优先级。
- `PlayerInventory`、`Items`、`ItemJar`、Glazier/Sleek 类型。
- LMN handler、队列、限流器和连接对象。
- JSON/配置文件路径、锁对象、线程调度器。
- 候选搜索缓存、对象池、UI 图层实现和动画。
- 原版 `removeItem/addItem` 调用细节。

## 9. 前端消费顺序

1. 读取 `FeatureStatusView`，只为 `Running` 功能启用交互。
2. 前端采集光标、容器几何和旋转输入，调用进程内候选 interface。
3. 渲染 `ItemPlacementPreview`；颜色与动画属于 Gemini。
4. 释放时把候选元组交给原生提交 adapter。
5. 观察原版库存投影变化；按 GPT-13 的 2 秒视觉等待与关联规则收敛，且不得假定存在逐次 ACK。
6. 设置 UI 从构建期静态 Settings facet 生成控件骨架，通过设置 command 提交，并以运行时完整 `FeatureSettingsSnapshot`/changed event 的 revision 更新有效值；GPT-13 规定总计 8 秒无终态后可通过 `0x0104` 请求一次完整快照。

## 10. 验收门禁

- Gemini 已完成当前版本的事后逐项复核并确认无缺失字段或 UI 类型泄漏；实现前仍需用编译桩验证只依赖本规格 DTO。
- 编译时验证客户端与 U3DS 引用交集。
- 序列化、限流、版本降级和模块隔离必须有自动化测试。
- SP、SteamP2PFriends Host/Client、U3DS 必须使用同一候选 DLL 哈希独立验证。
- 在上述门禁完成前，本规格不得标记为 Stable。


